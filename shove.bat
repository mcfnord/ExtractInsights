setlocal EnableDelayedExpansion

set "last_day_sampled="

:mainLoop
    scp -i oregon.pem predicted.json ubuntu@35.89.188.108:/home/ubuntu/predicted.json

    for /f "tokens=1" %%a in ('date /t') do set "current_day=%%a"

    if "!current_day!" equ "!last_day_sampled!" (
        echo equal
    ) else (
        set "last_day_sampled=!current_day!"
        echo Not equal: Different day from last sample.
        curl https://jamulus.live/census_uniq.csv > census_uniq.csv
        curl https://jamulus.live/census_metadata.csv > census_metadata.csv
        curl https://jamulus.live/servers_metadata.csv > servers_metadata.csv
        del cooked.json
    )

    echo Sleeping...
    timeout /nobreak /t 600 >nul
goto mainLoop
