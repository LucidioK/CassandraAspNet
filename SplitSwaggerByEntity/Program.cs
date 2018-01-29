using System;
using System.Collections.Generic;

namespace SplitSwaggerByEntity
{
    class Program
    {
        static void Main(string[] args)
        {
            
            Utils.Utils.WriteLineGreen("SplitSwaggerByEntity start");
            Utils.Utils.ShowHelpAndAbortIfNeeded(args, 2, $@"


SplitSwaggerByEntity swaggerFile outputFolder
Splits swagger json file into separated files by entity.
Example:

    SplitSwaggerByEntity c:\temp\PetStore.json c:\temp\Entities

Would create the following files:

    c:\temp\Entities\PetStorePet.json
    c:\temp\Entities\PetStoreOrder.json
    c:\temp\Entities\PetStoreUser.json


");
            var gen = new SwaggerSplitter(args[0], args[1]);
            gen.Generate();
            Utils.Utils.WriteLineGreen("SplitSwaggerByEntity end");
        }
    }
}
