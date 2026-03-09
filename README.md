## SqlNotebook

SqlNotebook — это веб‑приложение для работы с SQL‑блокнотами: выполнение запросов, визуализация (Chart, Mermaid, Excalidraw), рабочие пространства, статусы ноутов, теги и управление доступом.

### Стек
- .NET 8, ASP.NET Core (`SqlNotebook.Service`)
- Blazor WebAssembly + MudBlazor (`SqlNotebook.UI`)
- EF Core + SQL Server
- Дополнительный сервис `ai-sql-service` (Python, FastAPI/Uvicorn) для AI‑подсказок

### Лицензия (MIT)
Проект распространяется по лицензии **MIT**.  
Вы можете:
- использовать, копировать, изменять, сливать, публиковать, распространять, сублицензировать и/или продавать копии ПО;
- при условии, что во всех копиях или значимых частях ПО сохраняется следующий копирайт и разрешение:

```text
Copyright (c) 2026 SqlNotebook contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

### Локальный запуск (без Docker)
```bash
dotnet build SqlNotebook.sln
dotnet run --project SqlNotebook.Service/SqlNotebook.Service.csproj
```

По умолчанию backend поднимается на `http://localhost:5175`, Blazor UI — на том же хосте.

### Запуск в Docker

1. Собрать образы:
```bash
docker compose build
```

2. Запустить:
```bash
docker compose up
```

По умолчанию:
- SQL Server: `localhost,14333`
- Backend + UI: `http://localhost:5175`
- AI‑сервис: `http://localhost:8000`

### Сборка на GitHub

В репозитории есть workflow `.github/workflows/dotnet-ci.yml`, который:
- запускается на `push` и `pull_request` в ветку `main`;
- собирает решение `SqlNotebook.sln` под .NET 8;
- прогоняет юнит‑тесты (если будут добавлены тестовые проекты);
- (опционально) может собирать Docker‑образы и пушить их в контейнерный реестр GitHub.

