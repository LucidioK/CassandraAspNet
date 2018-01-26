Echo starting the CassandraLocal container, from scratch
docker container rm CassandraLocal --force
docker pull cassandra
docker run --name CassandraLocal -it -d -p 9042:9042 cassandra:3.11
docker exec --privileged -it CassandraLocal apt update

Echo Opening port 9042 in the docker container.
Echo Important: answer Y when you are asked to save IPv4 and IPV6 configs.
docker exec --privileged -it CassandraLocal apt install --yes iptables-persistent     
docker exec --privileged -it CassandraLocal iptables -A INPUT -p tcp --dport 9042 -j ACCEPT 

Echo Creating keyspace pse
docker exec --privileged -it CassandraLocal cqlsh -e "create keyspace if not exists pse with replication = {'class':'SimpleStrategy', 'replication_factor':1};"

Echo Creating table pse.payment_history
docker exec --privileged -it CassandraLocal cqlsh -e "create table if not exists pse.payment_history (key bigint, contract_account_id bigint, activity_date date, amount decimal, payment_id varchar, sequence int, PRIMARY KEY(key));"

Echo Creating index pse_payment_history_contract_account_id
docker exec --privileged -it CassandraLocal cqlsh -e "create index if not exists pse_payment_history_contract_account_id on PSE.payment_history (contract_account_id);"

Echo inserting data.
docker exec --privileged -it CassandraLocal cqlsh -e "insert into pse.payment_history (key, contract_account_id, activity_date, amount, payment_id, sequence) values (1,101,'2018-01-01', 10.01, 'ID01', 1);"

docker exec --privileged -it CassandraLocal cqlsh -e "insert into pse.payment_history (key, contract_account_id, activity_date, amount, payment_id, sequence) values (2,102,'2018-01-01', 10.02, 'ID02', 2);"

docker exec --privileged -it CassandraLocal cqlsh -e "insert into pse.payment_history (key, contract_account_id, activity_date, amount, payment_id, sequence) values (3,103,'2017-11-01', 10.03, 'ID03', 3);"

docker exec --privileged -it CassandraLocal cqlsh -e "insert into pse.payment_history (key, contract_account_id, activity_date, amount, payment_id, sequence) values (4,103,'2017-12-01', 10.03, 'ID04', 4);"

docker exec --privileged -it CassandraLocal cqlsh -e "insert into pse.payment_history (key, contract_account_id, activity_date, amount, payment_id, sequence) values (5,103,'2018-01-01', 10.05, 'ID05', 5);"
Echo Done.
