REM @echo off

SET RENV=../App/R#.exe
SET MAN="../docs/documents"

"%RENV%" --man.1 --debug --out %MAN%

pause