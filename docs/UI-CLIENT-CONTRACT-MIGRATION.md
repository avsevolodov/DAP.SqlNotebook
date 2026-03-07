# Миграция: UI ↔ Controller через Service.Client, маршруты в MainRoutes, DTO в Contract

## Принципы
- Все обращения UI к API — только через **SqlNotebook.Service.Client** (клиенты по аналогии с IWorkspaceManager).
- Все UI-маршруты (страницы, ссылки) — в **MainRoutes**.
- Все DTO — в **SqlNotebook.Contract** (уже выполнено).

## Шаги

### 1. MainRoutes — все UI-маршруты
- Добавить: SchemaCatalogEntities, Notebooks с query (workspaceId), прочие константы/хелперы для ссылок, которые сейчас захардкожены в UI.

### 2. EndpointsHelper — пути API
- Добавить базовый путь Catalog (`api/v1/catalog`) и хелперы для: nodes, databases, connection-status, entities, fields, import-structure, entity select-text.

### 3. ICatalogManager + CatalogManager (Service.Client)
- Реализовать все вызовы Catalog API; использовать Contract DTO; регистрировать в DI UI.

### 4. JsonHelper (Service.Client) — десериализация статуса
- При необходимости: конвертер для ConnectionStatusInfo (число/строка), чтобы статус источника работал при ответах через клиент.

### 5. UI: замена Http на ICatalogManager
- SourceView, SourceEdit, SchemaCatalog, SchemaCatalogEntities, TableView, SchemaTree, Notebook.razor (часть каталога) — использовать только ICatalogManager.

### 6. IAiAssistClient + AiAssistClient
- EndpointsHelper для ai/assist; методы: sessions, messages, send. UI AiAgentPanel — только через IAiAssistClient.

### 7. Suggest-chart и прочие AI/catalog в Notebook
- Вызов suggest-chart и загрузка узлов каталога в Notebook.razor — через клиенты (CatalogManager / расширение NotebookManager или отдельный клиент).

### 8. Замена хардкодных маршрутов на MainRoutes ✅
- WorkspaceTreeNode, Workspaces, NotebooksGrid, Breadcrumbs, SchemaCatalog, SchemaCatalogEntities — все ссылки через MainRoutes (GetNotebooksRoute, SchemaCatalogEntities, SchemaCatalog).

---

## Выполнено в этой сессии
- **MainRoutes**: добавлены `SchemaCatalogEntities`, `GetNotebooksRoute(workspaceId?)`.
- **EndpointsHelper**: базовый путь Catalog и хелперы для nodes, databases, entities (в т.ч. paged), entity/fields, connection-status, import-structure, entity select-text.
- **ICatalogManager + CatalogManager** в Service.Client; регистрация в UI AddClients.
- **UI (Catalog)**: SourceView, SourceEdit, SchemaCatalog, SchemaCatalogEntities, TableView, SchemaTree, Notebook.razor — все вызовы каталога переведены на `ICatalogManager`.
- **Маршруты**: все ссылки на notebooks и schema-catalog используют MainRoutes (WorkspaceTreeNode, Workspaces, NotebooksGrid, Breadcrumbs, SchemaCatalog, SchemaCatalogEntities).

## Осталось
- ~~**Шаги 6–7**: IAiAssistClient + AiAssistClient; suggest-chart через клиент; после этого убрать оставшиеся прямые вызовы Http в AiAgentPanel и Notebook.razor.~~ **Сделано.**

### Выполнено дополнительно (шаги 6–7)
- **EndpointsHelper**: добавлены `AiAssist`, `AiSql`, `AiAssistSessions(notebookId?)`, `AiAssistMessages(sessionId?)`, `AiAssistSend()`, `AiSqlSuggestChart()`.
- **IAiAssistClient + AiAssistClient**: GetSessionsAsync, CreateSessionAsync, GetMessagesAsync, SendAsync; зарегистрированы в UI.
- **IAiSqlClient + AiSqlClient**: SuggestChartAsync (при ошибке возвращает null); зарегистрированы в UI.
- **AiAgentPanel**: все вызовы переведены на `IAiAssistClient`; при ошибке Send показывается сообщение из исключения.
- **Notebook.razor**: suggest-chart вызывается через `IAiSqlClient.SuggestChartAsync`; `HttpClient` удалён из инжектов.
