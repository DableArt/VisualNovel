using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace VisualNovel.Novel.Tests
{
    /// <summary>
    /// EditMode-тесты ядра визуальной новеллы (Unity Test Runner → EditMode).
    /// Ядро — чистый C#, сцена не нужна.
    /// </summary>
    public sealed class NovelSessionTests
    {
        private sealed class SpyAudio : IAudioService
        {
            public readonly List<string> Music = new();
            public readonly List<string> Sfx = new();
            public readonly List<string> Voice = new();

            public void PlayMusic(string musicId) => Music.Add(musicId);
            public void StopMusic() { }
            public void PlaySfx(string sfxId) => Sfx.Add(sfxId);
            public void PlayVoice(string voiceId) => Voice.Add(voiceId);
        }

        private NovelCoreConfig config;
        private SpyAudio audio;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<NovelCoreConfig>();
            config.maxChoices = NovelCoreConfig.HardMaxChoices;
            config.choiceTimeLimit = 5f;
            config.idleChoiceText = "(Промолчать)";
            config.idleNextNodeId = "idle_end";
            audio = new SpyAudio();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(config);

        private static string Node(string id, string timeoutTarget, params string[] choiceTargets)
        {
            var choices = new List<string>();
            for (int i = 0; i < choiceTargets.Length; i++)
                choices.Add($"{{\"text\":\"Вариант {i + 1}\",\"nextNodeId\":\"{choiceTargets[i]}\"}}");

            return $"{{\"id\":\"{id}\",\"text\":\"Текст {id}\",\"timeoutNextNodeId\":\"{timeoutTarget}\"," +
                   $"\"choices\":[{string.Join(",", choices)}]}}";
        }

        private NovelSession CreateSession(params string[] nodes)
        {
            string json = "{\"startNodeId\":\"" + "start" + "\",\"nodes\":[" + string.Join(",", nodes) + "]}";
            return new NovelSession(config, new JsonDialogueProvider(json), audio);
        }

        [Test]
        public void Presents_No_More_Than_MaxChoices()
        {
            config.maxChoices = 3;
            var session = CreateSession(Node("start", "", "a", "b", "c", "d", "e", "f", "g", "h"));

            IReadOnlyList<ChoiceOption> presented = null;
            session.ChoicesPresented += (options, _) => presented = options;
            session.Start();

            Assert.AreEqual(3, presented.Count);
            Assert.AreEqual(3, session.PresentedChoices.Count);
        }

        [Test]
        public void Presents_Up_To_Hard_Max_6()
        {
            var session = CreateSession(Node("start", "", "a", "b", "c", "d", "e", "f", "g", "h"));
            session.Start();

            Assert.AreEqual(6, session.PresentedChoices.Count);
        }

        [Test]
        public void SelectChoice_Navigates_And_Fires_Events()
        {
            var session = CreateSession(
                Node("start", "", "second"),
                Node("second", "", ""));

            ChoiceOption resolved = null;
            bool byTimeout = true;
            session.ChoiceResolved += (option, timeout) => { resolved = option; byTimeout = timeout; };
            session.Start();

            Assert.IsTrue(session.SelectChoice(0));
            Assert.AreEqual("second", session.CurrentNode.id);
            Assert.AreEqual("Вариант 1", resolved.text);
            Assert.IsFalse(byTimeout);
        }

        [Test]
        public void Timeout_Selects_Idle_Option_From_Config()
        {
            var session = CreateSession(
                Node("start", "", "a"),
                Node("idle_end", "", ""));

            ChoiceOption resolved = null;
            bool byTimeout = false;
            session.ChoiceResolved += (option, timeout) => { resolved = option; byTimeout = timeout; };
            session.Start();
            session.BeginChoiceTimer();

            session.Tick(config.choiceTimeLimit + 0.1f);

            Assert.IsTrue(byTimeout);
            Assert.AreEqual("(Промолчать)", resolved.text);
            Assert.AreEqual("idle_end", session.CurrentNode.id);
        }

        [Test]
        public void Timeout_Node_Override_Beats_Config()
        {
            var session = CreateSession(
                Node("start", "node_idle", "a"),
                Node("node_idle", "", ""),
                Node("idle_end", "", ""));

            session.Start();
            session.BeginChoiceTimer();
            session.Tick(config.choiceTimeLimit + 0.1f);

            Assert.AreEqual("node_idle", session.CurrentNode.id);
        }

        [Test]
        public void No_Timeout_When_Timer_Disabled()
        {
            config.choiceTimeLimit = 0f;
            var session = CreateSession(Node("start", "", "a"));
            session.Start();
            session.BeginChoiceTimer();

            session.Tick(1000f);

            Assert.AreEqual(NovelSession.State.AwaitingChoice, session.CurrentState);
        }

        [Test]
        public void Node_Without_Choices_Finishes_Session()
        {
            var session = CreateSession(Node("start", ""));

            bool finished = false;
            session.SessionFinished += () => finished = true;
            session.Start();

            Assert.IsTrue(finished);
            Assert.AreEqual(NovelSession.State.Finished, session.CurrentState);
        }

        [Test]
        public void Timeout_Without_Target_Finishes_Session()
        {
            config.idleNextNodeId = string.Empty;
            var session = CreateSession(Node("start", "", "a"));

            bool finished = false;
            session.SessionFinished += () => finished = true;
            session.Start();
            session.BeginChoiceTimer();
            session.Tick(config.choiceTimeLimit + 0.1f);

            Assert.IsTrue(finished);
        }

        [Test]
        public void Audio_Cues_Played_On_NodeEnter()
        {
            string json = "{\"startNodeId\":\"start\",\"nodes\":[{" +
                          "\"id\":\"start\",\"text\":\"Т\"," +
                          "\"musicId\":\"event:/Music/Theme\",\"sfxId\":\"event:/Sfx/Hit\",\"voiceId\":\"voice_01\"," +
                          "\"choices\":[{\"text\":\"Дальше\",\"nextNodeId\":\"\"}]}]}";
            var session = new NovelSession(config, new JsonDialogueProvider(json), audio);
            session.Start();

            CollectionAssert.AreEqual(new[] { "event:/Music/Theme" }, audio.Music);
            CollectionAssert.AreEqual(new[] { "event:/Sfx/Hit" }, audio.Sfx);
            CollectionAssert.AreEqual(new[] { "voice_01" }, audio.Voice);
        }

        [Test]
        public void Unknown_Node_Finishes_With_Error()
        {
            var session = CreateSession(Node("start", "", "missing_node"));

            string error = null;
            bool finished = false;
            session.Error += msg => error = msg;
            session.SessionFinished += () => finished = true;

            session.Start();
            session.SelectChoice(0);

            Assert.IsNotNull(error);
            Assert.IsTrue(finished);
        }

        [Test]
        public void Timer_Waits_For_BeginChoiceTimer()
        {
            var session = CreateSession(
                Node("start", "", "a"),
                Node("idle_end", ""));
            session.Start();

            // UI ещё появляется — таймер взведён, но не идёт.
            Assert.IsFalse(session.ChoiceTimerRunning);
            session.Tick(config.choiceTimeLimit + 0.1f);
            Assert.AreEqual(NovelSession.State.AwaitingChoice, session.CurrentState);
            Assert.AreEqual(config.choiceTimeLimit, session.RemainingChoiceTime);

            // UI появился — отсчёт пошёл.
            session.BeginChoiceTimer();
            Assert.IsTrue(session.ChoiceTimerRunning);
            session.Tick(config.choiceTimeLimit + 0.1f);
            Assert.AreEqual("idle_end", session.CurrentNode.id);
        }

        [Test]
        public void BeginChoiceTimer_Is_Safe_When_Repeated_Or_Disabled()
        {
            config.choiceTimeLimit = 0f;
            var session = CreateSession(Node("start", "", "a"));
            session.Start();

            session.BeginChoiceTimer(); // таймер выключен — без эффекта
            Assert.IsFalse(session.ChoiceTimerRunning);
            Assert.AreEqual(NovelSession.State.AwaitingChoice, session.CurrentState);
        }

        [Test]
        public void Node_Time_Limit_Override_Beats_Config()
        {
            string json = "{\"startNodeId\":\"start\",\"nodes\":[{" +
                          "\"id\":\"start\",\"text\":\"Т\",\"timeLimitOverride\":45," +
                          "\"choices\":[{\"text\":\"Дальше\",\"nextNodeId\":\"\"}]}]}";
            var session = new NovelSession(config, new JsonDialogueProvider(json), audio);

            float presentedLimit = -1f;
            session.ChoicesPresented += (_, limit) => presentedLimit = limit;
            session.Start();

            Assert.AreEqual(45f, presentedLimit);
            Assert.AreEqual(45f, session.CurrentChoiceTimeLimit);
            Assert.AreEqual(45f, session.RemainingChoiceTime);

            session.BeginChoiceTimer();
            session.Tick(config.choiceTimeLimit + 0.1f); // больше конфига, но меньше override
            Assert.AreEqual(NovelSession.State.AwaitingChoice, session.CurrentState);
        }

        [Test]
        public void Choice_Bonus_And_Chance_Load_From_Json()
        {
            string json = "{\"startNodeId\":\"start\",\"nodes\":[{" +
                          "\"id\":\"start\",\"text\":\"Т\",\"choices\":[" +
                          "{\"text\":\"Атака\",\"nextNodeId\":\"\",\"bonusText\":\"+10 к мастерству\",\"successChance\":0.25}," +
                          "{\"text\":\"Уйти\",\"nextNodeId\":\"\"}]}]}";
            var session = new NovelSession(config, new JsonDialogueProvider(json), audio);
            session.Start();

            var first = session.PresentedChoices[0];
            Assert.AreEqual("+10 к мастерству", first.bonusText);
            Assert.AreEqual(0.25f, first.successChance, 0.0001f);

            var second = session.PresentedChoices[1];
            Assert.AreEqual(string.Empty, second.bonusText);
            Assert.Less(second.successChance, 0f); // не задан — не показывается
        }

        [Test]
        public void Second_Tick_After_Timeout_Does_Not_Resolve_Twice()
        {
            var session = CreateSession(
                Node("start", "", "a"),
                Node("idle_end", ""));

            int resolvedCount = 0;
            session.ChoiceResolved += (_, _) => resolvedCount++;
            session.Start();
            session.BeginChoiceTimer();
            session.Tick(config.choiceTimeLimit + 0.1f);
            session.Tick(config.choiceTimeLimit + 0.1f);

            Assert.AreEqual(1, resolvedCount);
            Assert.AreEqual(NovelSession.State.Finished, session.CurrentState);
        }
    }
}
