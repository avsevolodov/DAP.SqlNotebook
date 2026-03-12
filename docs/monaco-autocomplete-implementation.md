# Monaco SQL Autocomplete — план реализации и выполненные шаги

## Цели

1. **Окно подсказок показывается при наборе** — не только по Ctrl+Space.
2. **Замена только префикса** — при `sele|` и подсказке `select` заменяется только `sele`, а не целое слово `SELECT`.
3. **Курсор передаётся на бэкенд** — Python и C# получают точную позицию для контекста.

---

## Выполненные шаги

### 1. Включить автопоказ подсказок при наборе (Monaco)

- **Файл:** `SqlNotebook.Service/Components/App.razor`
- **Изменение:** В опциях `monaco.editor.create` добавлено:
  - `quickSuggestions: { other: true, comments: false, strings: false }` — подсказки по буквам в коде.
  - `suggestOnTriggerCharacters: true` — показ по символам из `triggerCharacters`.
- **Итог:** Dropdown открывается при наборе текста и по пробелу/точке/запятой, не только по Ctrl+Space.

### 2. Передавать `cursor_position` в AI autocomplete

- **Файл:** `SqlNotebook.Service/Components/App.razor`
- **Изменение:**
  - Введена функция `getCursorOffset(model, position)` — считает символьный offset (0-based) через `model.getOffsetAt(position)` или вручную по строкам.
  - В теле запроса к `/api/v1/ai/sql/autocomplete` добавлено поле `cursor_position: getCursorOffset(model, position)`.
- **Итог:** Бэкенд (C# → Python) получает позицию курсора и корректно определяет контекст (prefix, FROM, SELECT_LIST и т.д.).

### 3. Единый диапазон замены по префиксу (Monaco API)

- **Файл:** `SqlNotebook.Service/Components/App.razor`
- **Изменение:**
  - Функция `getReplaceRange(model, position)` использует `model.getWordUntilPosition(position)` и строит `Range` от `word.startColumn` до `position.column`.
  - Оба провайдера (schema и AI) используют один и тот же `getReplaceRange(...)` для `range` в каждом `CompletionItem`.
- **Итог:** При вставке подсказки заменяется только текущий префикс (например, `sele`), а не всё слово до границ токена.

### 4. Унификация запроса и диапазона

- Оба провайдера вызывают `getReplaceRange(model, position)` для `range`.
- AI-провайдер передаёт в запросе `cursor_position`; контракт C# (`AiSqlAutocompleteRequestInfo.CursorPosition`) и маппинг в BL/HTTP уже поддерживают это поле.

---

## Проверка

1. Набрать `sele` в редакторе SQL — окно подсказок должно открыться без Ctrl+Space.
2. Выбрать `select` — в строке должно остаться `select` (заменены только 4 символа `sele`).
3. В DevTools (Network) для запроса к `autocomplete` в body должен быть `cursor_position` (число).
4. При `c.|` и подсказке `c.email` заменяется только фрагмент после точки (пустой или частичный префикс), а не весь текст строки.

---

## Зависимости

- **Backend:** C# API уже принимает `CursorPosition` и передаёт его в Python; изменений в контрактах не требуется.
- **Monaco:** версия 0.45.0 (поддерживает `getWordUntilPosition`, `getOffsetAt`).
