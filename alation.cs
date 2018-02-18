using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;
using NUnitLite;

internal class JsonSerializer<T> 
{
    private DataContractJsonSerializer ser;  

    internal JsonSerializer()
    {
        ser = new DataContractJsonSerializer(typeof(T));  
    }
    
    internal string Serialize(T t){
        MemoryStream ms = new MemoryStream();         
        ser.WriteObject(ms, t);  
        ms.Position = 0;        
        StreamReader sr = new StreamReader(ms);  
        return sr.ReadToEnd(); 
    }

    internal T Deserialize(string json){
        MemoryStream ms = new MemoryStream(Encoding.ASCII.GetBytes(json)); 
        ms.Position = 0;        
        return (T)ser.ReadObject(ms); 
    }
}

[DataContract]  
internal class ScoredEntity 
{  
    [DataMember]  
    internal string name;  

    [DataMember]  
    internal int score;  
        
    internal static void PrintList(List<ScoredEntity> list)
    {
        foreach(var e in list)
        {
            Console.WriteLine($"name={e.name}, score={e.score}");
        }
    }
    
    internal static int CompareEntitiesByScore(ScoredEntity x, ScoredEntity y)
    {
        if (x == null)
        {
            if (y == null)
            {
                // If x is null and y is null, 
                // they're equal. 
                return 0;
            }
            else
            {
                // If x is null and y is not null,
                // y is greater. 
                return -1;
            }
        }
        else
        {
            // If x is not null...
            //
            if (y == null)
                // ...and y is null, x is greater.
            {
                return 1;
            }
            else
            {
                // ...and y is not null, compare the 
                // scores of the two Persons.
                //
                return  x.score.CompareTo(y.score);
            }
        }
    }
}  

[DataContract] 
internal class trie<T>
{
    [DataMember]
    List<T> values;
    
    [DataMember]
    int maxResults;

    [DataMember]
    Dictionary<char, trie<T>> edges;
    
    internal trie(int maxResults)
    {
        this.maxResults = maxResults;
        values = new List<T>(maxResults);
        edges = new Dictionary<char, trie<T>>();
    }
    
    internal trie<T> Add(string s, T value)
    {        
        if(values.Count < this.maxResults)
        {
            if(values.Count == 0 || !values[values.Count-1].Equals(value))
            {
                values.Add(value);
            }
        }
        if(!edges.ContainsKey(s[0]))
        {
            edges[s[0]] = new trie<T>(maxResults);
        }
        if(s.Length>1)
        {
            edges[s[0]].Add(s.Substring(1,s.Length-1), value);
        }
        return this;
    }
    
    internal List<T> Retrieve(string s)
    {
        if(s.Length>0)
        {
            if(!edges.ContainsKey(s[0]))
            {
                return new List<T>();
            }
            return edges[s[0]].Retrieve(s.Substring(1,s.Length-1));
        }
        return values;        
    }
}


public class Runner {
    public static int Main(string[] args) {
        return new AutoRun(Assembly.GetCallingAssembly()).Execute(new String[] {"--labels=All"});
    }

    [TestFixture]
    public class ScoredEntityTests {
    
        [Test]
        public void CompareEntitiesByScoreBothNullReturns0() {
            ScoredEntity e1 = null;
            ScoredEntity e2 = null;
            int result = ScoredEntity.CompareEntitiesByScore(e1,e2);
            Assert.AreEqual(result,0);
        }

        [Test]
        public void CompareEntitiesByScoreFirstNullReturnsMinus1() {
            ScoredEntity e1 = null;
            ScoredEntity e2 = new ScoredEntity();
            int result = ScoredEntity.CompareEntitiesByScore(e1,e2);
            Assert.AreEqual(result,-1);
        }

        [Test]
        public void CompareEntitiesByScoreSecondNullReturns1() {
            ScoredEntity e1 = new ScoredEntity();
            ScoredEntity e2 = null;
            int result = ScoredEntity.CompareEntitiesByScore(e1,e2);
            Assert.AreEqual(result,1);
        }

        [Test]
        public void CompareEntitiesByScoreEqualReturns0() {
            ScoredEntity e1 = new ScoredEntity();;
            ScoredEntity e2 = new ScoredEntity();;
            int result = ScoredEntity.CompareEntitiesByScore(e1,e2);
            Assert.AreEqual(result,0);
        }

        [Test]
        public void CompareEntitiesByScoreFirstGreaterReturns1() {
            ScoredEntity e1 = new ScoredEntity();
            e1.score = 2;
            ScoredEntity e2 = new ScoredEntity();
            e2.score = 1;
            int result = ScoredEntity.CompareEntitiesByScore(e1,e2);
            Assert.AreEqual(result,1);
        }

        [Test]
        public void CompareEntitiesByScoreSecondGreaterReturnsMinus1() {
            ScoredEntity e1 = new ScoredEntity();
            e1.score = 1;
            ScoredEntity e2 = new ScoredEntity();
            e2.score = 2;
            int result = ScoredEntity.CompareEntitiesByScore(e1,e2);
            Assert.AreEqual(result,-1);
        }

    }

    [TestFixture]
    public class MainTests {
        bool testFixtureSetUp = false;
        List<ScoredEntity> entities;
        trie<int> radixEntities;
    
        [SetUp]
        public void SetUpScoredEntities(){
            //This version of NUnit does not support TestFixtureSetUp 
            if(!testFixtureSetUp) {
                // Create a list.
                entities = GenerateList(20);

                Console.WriteLine("Original list");
                ScoredEntity.PrintList(entities);
                Console.WriteLine();

                //Sort list and make radix tree.
                entities.Sort(ScoredEntity.CompareEntitiesByScore);
                radixEntities = new trie<int>(10);
                int index = 0;
                foreach(var entity in entities)
                {
                    String[] names = entity.name.ToLower().Split('_');
                    foreach(var name in names)
                    {
                        radixEntities.Add(name, index);
                    }
                    ++index;
                }

                testFixtureSetUp = true;
            }
        }
        
        internal static List<ScoredEntity> GenerateList(int length)
        {
            var entities = new List<ScoredEntity>();
            for(int i=0;i<length;++i){
                ScoredEntity e = new ScoredEntity();
                char[] chars = new char[5];
                chars[0] = (char)('A'+i);
                chars[1] = (char)('z'-i);
                chars[2] = '_';
                chars[3] = (char)('Z'-i);
                chars[4] = (char)('a'+i);
                e.name = new String(chars);  
                e.score = length-i;
                entities.Add(e);
            }
            return entities;
        }
        
        [Test]
        public void SerializeDeserialize(){
            var jsEntities = new JsonSerializer<List<ScoredEntity>>();
            string json = jsEntities.Serialize(entities);
            entities = jsEntities.Deserialize(json);
            Assert.IsNotNull(entities);
            Assert.AreEqual(entities.Count,20);

            var jsTrie = new JsonSerializer<trie<int>>();
            json = jsTrie.Serialize(radixEntities);
            radixEntities = jsTrie.Deserialize(json);
            Assert.IsNotNull(radixEntities);
        }
        
        [Test]
        public void QueryAReturnsOneResult(){
            string query = "a";
            List<int> result = radixEntities.Retrieve(query);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Az_Za", entities[result[0]].name);
        }

        [Test]
        public void QueryHReturnsOTwoResults(){
            string query = "h";
            List<int> result = radixEntities.Retrieve(query);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("Sh_Hs", entities[result[0]].name);
            Assert.AreEqual("Hs_Sh", entities[result[1]].name);
        }
    }
}