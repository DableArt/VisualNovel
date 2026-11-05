# Ядро визуальной новеллы (NovelCore)

Бэкенд визуальной новеллы: граф диалога, выбор ответа (максимум 6 —
настраивается), таймер выбора с вариантом «бездействие» по таймауту,
кью для анимированных сценок (после — персонажи в Idle), музыка/звук
и зарезервированное место под озвучку.

## Архитектура

Паттерн — **слои Core/View + Observer (события)**, как в модуле Training:
ядро не знает про Unity-визуал и полностью тестируется в EditMode.

```
Assets/Scripts/Novel/
├── Core/                          ← логика, не зависит от визуала
│   ├── NovelSession.cs            ← ядро: узлы, выборы, таймер, бездействие, события
│   ├── NovelCoreConfig.cs         ← SO-настройки: maxChoices (1..6), таймер, бездействие
│   ├── DialogueNode.cs            ← узел: текст, сценка, музыка/звук/голос, выборы
│   ├── ChoiceOption.cs            ← вариант ответа (текст + следующий узел)
│   ├── DialogueGraph.cs           ← граф узлов + стартовый узел
│   ├── IDialogueProvider.cs       ← Strategy: источник диалогов
│   ├── JsonDialogueProvider.cs    ← диалоги из JSON (Newtonsoft.Json)
│   ├── IAudioService.cs           ← Strategy: музыка / sfx / озвучка (зарезервировано)
│   └── NullAudioService.cs        ← заглушка-тишина по умолчанию
├── View/                          ← визуальная обвязка (Unity)
│   ├── NovelSessionRunner.cs      ← сборка сессии, Tick таймера, R3 ReactiveProperty времени
│   ├── ChoicePanelView.cs         ← до 6 кнопок + полоса таймера (uGUI + TMP)
│   ├── CharacterActorView.cs      ← проигрывает сценку → возврат в Idle (ожидание)
│   └── FmodAudioService.cs        ← мост к FMOD (не настроен — логи, игра не падает)
└── Tests/                         ← EditMode-тесты ядра (NUnit)
```

Принципы: ООП, KISS. Новые сцены добавляются данными (JSON), код не
трогаем. **Strategy** для источника диалогов и аудио: когда появится
сюжет, рядом встанет `InkDialogueProvider` поверх Ink/Fungus (InkFungus
NarrativeDirector уже в проекте) — ядро менять не придётся.

## Правила ядра

- Показывается не больше `maxChoices` вариантов (1..6, жёсткий потолок
  `NovelCoreConfig.HardMaxChoices = 6`); лишние варианты узла отсекаются.
- Таймер (`choiceTimeLimit`, сек; `0` — выключен) идёт, пока показаны выборы.
  По таймауту разыгрывается дополнительный вариант **«бездействие»**
  (текст — `idleChoiceText`): цель — `timeoutNextNodeId` узла, иначе
  `idleNextNodeId` конфига, иначе сцена завершается.
- Узел без вариантов ответа завершает сцену (`SessionFinished`).
- При входе в узел ядро бросает `NodeEntered` (сценки/фон/текст) и
  аудио-кью: `musicId`, `sfxId`, `voiceId` (озвучка зарезервирована).

## Настройка в Unity (6000.6.7f1)

1. **Конфиг**: готовый лежит в `Assets/Settings/Novel/NovelCoreConfig.asset`
   (или Create → VisualNovel → Novel → Core Config). Выставь
   `Max Choices`, `Choice Time Limit`, текст и узел бездействия.
2. **Диалог**: JSON по образцу `Assets/Resources/Novel/DemoDialogue.json`.
3. **Сцена** (Canvas, Screen Space):
   - Пустой объект `NovelRunner` + компонент **NovelSessionRunner**:
     Config, Dialogue Json (TextAsset), Choice Panel.
   - Панель выбора: объект-панель + 6 кнопок (Button с TMP_Text внутри),
     Slider для таймера. На панель — **ChoicePanelView**, перетащить
     Panel Root, 6 кнопок и Timer Bar.
   - Актёры: на объекты с Animator — **CharacterActorView**
     (Idle-состояние по умолчанию — `Idle`). Их список — в Runner → Actors.
     Сценки в Animator не должны лупиться.
   - Звук: добавь **FmodAudioService** и перетащи в Runner → Audio Service.
     Пока FMOD не настроен — только логи, ничего не ломается.

## Тесты

Unity Test Runner → EditMode → `VisualNovel.Novel.Tests`: лимит выборов,
навигация, таймаут/бездействие (узел-override и конфиг), выключенный
таймер, завершение сцены, аудио-кью, защита от двойного таймаута.
