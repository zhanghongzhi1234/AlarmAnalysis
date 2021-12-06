%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\installutil.exe AlarmAnalysisService.exe
Net Start AlarmAnalysisService
sc config AlarmAnalysisService start= auto