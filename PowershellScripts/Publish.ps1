$dnt='C:\Program Files\dotnet\dotnet.exe';
&$dnt ('publish',
        '--self-contained',
        '--runtime',       'win10-x64',
        '--configuration', 'Debug',
        '--verbosity',     'Minimal');
