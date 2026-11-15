# Ядро визуальной новеллы (NovelCore) + диалоговая система

Бэкенд визуальной новеллы и её UI: граф диалога, «колесо» выбора ответа
(4 направления), таймер выбора с обратным отсчётом и «бездействием»
по таймауту, кью для анимированных сценок, музыка/звук, место под озвучку.
Это основа Core Loop.

## Архитектура

Паттерн — **слои Core/View + Observer (события)**, **Strategy** для источника
диалогов и аудио. Ядро не знает про Unity-визуал и полностью тестируется
в EditMode. ООП, KISS: новые сцены добавляются данными (JSON), код не трогаем.
Когда появится сюжет — рядом встанет `InkDialogueProvider` поверх Ink/Fungus
(InkFungus уже в проекте), ядро менять не придётся.

```
Assets/Scripts/Novel/
├── Core/                          ← логика, не зависит от визуала
│   ├── NovelSession.cs            ← ядро: узлы, выборы, таймер, бездействие, события
│   ├── NovelCoreConfig.cs         ← SO-настройки: maxChoices, таймер (30с), бездействие
│   ├── DialogueNode.cs            ← узел: текст, сценка, аудио, timeLimitOverride, выборы
│   ├── ChoiceOption.cs            ← вариант: текст, узел, bonusText, successChance
│   ├── DialogueGraph.cs           ← граф узлов + стартовый узел
│   ├── IDialogueProvider.cs       ← Strategy: источник диалогов (JSON, позже Ink)
│   ├── JsonDialogueProvider.cs    ← диалоги из JSON (Newtonsoft.Json)
│   ├── IAudioService.cs           ← Strategy: музыка / sfx / озвучка (зарезервировано)
│   └── NullAudioService.cs        ← заглушка-тишина по умолчанию
├── View/                          ← визуальная обвязка (Unity)
│   ├── NovelSessionRunner.cs      ← композитор: сборка сессии, секвенс появления, таймаут
│   ├── DialogueWindowView.cs      ← текстовое окно (имя + реплика), FadeInDown
│   ├── ChoiceWheelView.cs         ← «колесо» из 4 кнопок, FadeInUp, подписи бонусов
│   ├── ChoiceButtonView.cs        ← кнопка: hover — белый фон, Active — инверсия цвета
│   ├── ChoiceTimerView.cs         ← «Элипс»: радиальная заливка по часовой + секунды
│   ├── DialogueInputController.cs ← WASD/стрелки + геймпад (D-Pad, стик)
│   ├── FadeMotion.cs              ← ease-out fade+сдвиг без твинеров
│   ├── ChoiceDirection.cs         ← Up/Right/Down/Left = порядок вариантов узла
│   ├── ChoicePanelView.cs         ← простая панель до 6 кнопок (legacy/отладка)
│   ├── CharacterActorView.cs      ← сценка → возврат в Idle (ожидание)
│   └── FmodAudioService.cs        ← мост к FMOD (не настроен — логи, игра не падает)
└── Tests/                         ← EditMode-тесты ядра (NUnit)
```

## Правила ядра

- Показывается не больше `maxChoices` вариантов (потолок 6; колесо занимает
  первые 4: Up, Right, Down, Left).
- Таймер (`choiceTimeLimit`, сек; `0` — выключен) взводится при показе
  вариантов, но **стартует только по `BeginChoiceTimer()`** — View вызывает
  его после анимаций появления. Узел может переопределить лимит
  (`timeLimitOverride`, 0 — из конфига).
- По таймауту разыгрывается вариант **«бездействие»** (`idleChoiceText`):
  цель — `timeoutNextNodeId` узла, иначе `idleNextNodeId` конфига,
  иначе сцена завершается. Временное решение раннера: лог в консоль +
  выход из игры (`Quit On Timeout`).
- Узел без вариантов завершает сцену (`SessionFinished`), финальная реплика
  показывается.
- У варианта: `bonusText` (описание бонуса при hover) и `successChance`
  (0..1; отрицательное — не показывать).

## Секвенс появления диалога (Core Loop)

1. **Текстовое окно** с именем персонажа — FadeInDown (сверху вниз).
2. **«Колесо» вариантов** — FadeInUp (снизу вверх); вместе с ним виден эллипс.
3. После появления — `BeginChoiceTimer()`: **«Элипс» убывает по часовой
   стрелке**, текст — обратный отсчёт секунд.
4. До появления реплики блоков управления не видно — колесо и эллипс
   появляются после текстового окна.
5. Ввод: мышь (клик), WASD, стрелки; геймпад (D-Pad/левый стик) уже работает.
   Состояния варианта: обычный — БЕЗ фона, белый текст; hover — белый фон,
   тёмный текст + подпись бонуса и % срабатывания (по макету);
   нажатие (Active) — чёрный фон, белый текст.
   С клавиатуры/геймпада: первое нажатие направления — hover (выбор),
   повторное в ту же сторону — активация варианта.
6. Таймаут → «время закончилось» в консоль и выход (временно).

## Сцена DialogSystem

Всё уже разведено на `Assets/Scenes/DialogSystem.unity`:

- Canvas `DialogSystem`: **NovelSessionRunner** (Config + Dialogue Json,
  ссылки на UI) и **ChoiceWheelView**.
- `DialogPanel`: **DialogueWindowView** (имя — `NameCharacter`, текст — `Text (TMP)`).
- `ButtonAnswerUP/Right/Down/Left`: **ChoiceButtonView** (цвета состояний,
  длительность вспышки нажатия — в инспекторе).
- `HoverViewBonus/Label*`: контейнеры бонусов (`LabelBonus` + опционально
  `PercentLabel`) — находятся автоматически по имени.
- `TimeLimeter`: **ChoiceTimerView** (`TimeIndicator` — эллипс, `TimeText`).
- `EventSystem`: **DialogueInputController**.

Настройки времени — в `Assets/Settings/Novel/NovelCoreConfig.asset`
(30 сек по умолчанию) или per-узел `timeLimitOverride` в JSON.
Диалог — `Assets/Resources/Novel/DemoDialogue.json`.

Позже окно реплики будет позиционироваться возле рта говорящего —
достаточно двигать RectTransform `DialogPanel` перед показом.

## Тесты

Unity Test Runner → EditMode → `VisualNovel.Novel.Tests`: лимит выборов,
навигация, гейт таймера (ждёт BeginChoiceTimer), per-узел лимит, бонусы/%
из JSON, таймаут/бездействие (узел-override и конфиг), выключенный таймер,
завершение сцены, аудио-кью, защита от двойного таймаута.
