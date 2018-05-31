#!/bin/bash
echo "Fetch " + $SCRIPT_URL

curl $SCRIPT_URL > script.sql

echo "Script fetched:"

cat script.sql

echo

echo "SQL CMD on the script:"

/opt/mssql-tools/bin/sqlcmd -S $SQL_SERVER -d $SQL_DB -U $SQL_USER_NAME -P $SQL_PASSWORD -i script.sql