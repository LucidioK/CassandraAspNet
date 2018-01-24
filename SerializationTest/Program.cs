using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace SerializationTest
{
    [JsonObject("xuxu")]
    public class Account
    {
        [JsonProperty(PropertyName = "$ref")]
        public int AccountId { get; set; }
        public string Name { get; set; }
        public string Street { get; set; }
        public string Number { get; set; }
        public string Extension { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
    }

    public class Order
    {
        public int Number;
        public Account Account;
        public float Amount;
        public DateTime Date;
        [JsonProperty(PropertyName = "responses")]
        public Dictionary<string, ResponseDescription> Responses;
    }

    public class ResponseDescription {
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }
    }
    class Program
    {
        static void Main(string[] args)
        {
            var a = new Account()
            {
                AccountId = 1,
                Name = "Barry Manilow",
                Street = "rua Qualquer",
                Number = "1234",
                City = "Porto Alegre",
                State = "RS",
                PostalCode = "90640"
            };
            var o = new Order()
            {
                Number = 1,
                Account = a,
                Amount = (float)10,
                Date = DateTime.Now,
                Responses = new Dictionary<string, ResponseDescription>()
                {
                    { "400", new ResponseDescription(){ Description = "Xuxu"} },
                    { "401", new ResponseDescription(){ Description = "Mumu"} },
                }
            };
            var s = JsonConvert.SerializeObject(o, Formatting.Indented);
            System.Console.WriteLine(s);
        }
    }
}
