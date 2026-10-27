# WinServiceSentry

<div align="center">

![Platform](https://img.shields.io/badge/Platform-Windows-0078D4?style=flat-square&logo=windows)
![Language](https://img.shields.io/badge/Language-C%23%2013%20%2F%20.NET%2010-512BD4?style=flat-square&logo=dotnet)
![License](https://img.shields.io/badge/License-MIT-blue?style=flat-square)
![Build](https://img.shields.io/badge/Build-Passing-brightgreen?style=flat-square)

**Высокоскоростной аудит служб Windows и маппинга Service Host PID через нативный Service Control Manager.**  
*Прямое обращение к `advapi32!EnumServicesStatusExW` без PowerShell Get-Service и WMI.*

</div>

---

## Архитектура

- Опрос Service Control Manager (`OpenSCManagerW`) с флагами `SC_MANAGER_ENUMERATE_SERVICE`.
- Прямой маршалинг массива структур `ENUM_SERVICE_STATUS_PROCESS` из неуправляемой памяти.
- Точное определение ассоциированного PID процесса (`dwProcessId`) для служб, хостящихся в `svchost.exe`.

## Сборка и запуск

```powershell
dotnet build -c Release
.\bin\Release\net10.0\WinServiceSentry.exe
```

## Лицензия
MIT
