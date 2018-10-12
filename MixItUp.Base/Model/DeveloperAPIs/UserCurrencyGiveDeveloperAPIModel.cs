﻿using System.Runtime.Serialization;

namespace MixItUp.Base.Model.DeveloperAPIs
{
    [DataContract]
    public class UserCurrencyGiveDeveloperAPIModel
    {
        [DataMember]
        public int Amount { get; set; }

        [DataMember]
        public string UsernameOrID { get; set; }
    }
}