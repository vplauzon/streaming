curl $SCRIPT-URL > script.sql

sqlcmd -S $SQL-SERVER -d $SQL-DB -U $SQL-USER-NAME -P  SQL-PASSWORD -i script.sql