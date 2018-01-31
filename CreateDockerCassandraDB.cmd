Echo starting the CassandraLocal container, from scratch
docker container rm CassandraLocal --force
docker pull cassandra
docker run --name CassandraLocal -it -d -p 9042:9042 cassandra:3.11
docker exec --privileged -it CassandraLocal apt update

Echo Opening port 9042 in the docker container.
Echo Important: answer Y when you are asked to save IPv4 and IPV6 configs.
docker exec --privileged -it CassandraLocal apt install --yes iptables-persistent     
docker exec --privileged -it CassandraLocal iptables -A INPUT -p tcp --dport 9042 -j ACCEPT 


