docker container rm CassandraLocal --force;
docker pull cassandra;
docker run --name CassandraLocal -it -d -p 9042:9042 cassandra:3.11
docker exec --privileged -it CassandraLocal apt  update

docker exec --privileged -it CassandraLocal apt install --yes iptables-persistent;
docker exec --privileged -it CassandraLocal iptables -A INPUT -p tcp --dport 9042 -j ACCEPT;
