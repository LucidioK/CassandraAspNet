
Add-Type -Path 'C:\Users\lucidio.kuhn\.nuget\packages\cassandracsharpdriver\3.4.0.1\lib\net45\Cassandra.dll'
$b=[Cassandra.Cluster]::Builder();
$b2=$b.WithConnectionString("Contact Points = localhost; Port = 9042");
# Then the folowing line fails:
$b3=$b2.Build()
# Error message:
# Exception calling "Build" with "0" argument(s): "The type initializer for 'Cassandra.AtomicMonotonicTimestampGenerator' threw an exception."
#At line:1 char:1
# + $b2.Build()
# + ~~~~~~~~~~~~
#    + CategoryInfo          : NotSpecified: (:) [], MethodInvocationException
#    + FullyQualifiedErrorId : TypeInitializationException