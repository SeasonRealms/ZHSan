using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GameObjects
{
    [DataContract]
    public class TreasureCreationSetting : GameObject
    {
        //[DataMember]
        //public int ID { get; set; }

        //[DataMember]
        //public string Name { get; set; }

        [DataMember]
        public List<int> EligibleInfluenceIDs { get; set; } = new List<int>();

        [DataMember]
        public int TreasureGroup { get; set; }

        [DataMember]
        public int Cost { get; set; }

        [DataMember]
        public List<int> PicIDs { get; set; } = new List<int>();
    }
}