// See https://aka.ms/new-console-template for more information

using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddUserSecrets<Program>().Build();

const int RUN_COUNT = 10000;

var connectionString = config.GetSection("Probability")["ConnectionString"];

var query = @"
with rand AS (SELECT id, random=RAND() FROM Tiers)
   , cumsum AS (SELECT id, cum_sum=SUM(probability) Over (Order By id) from Tiers)
Select TOP 1 t.id,
             t.name,
             t.probability
FROM Tiers t
         inner join rand r on t.id = r.id
         inner join cumsum c on t.id = c.id
WHERE c.cum_sum - r.random >= 0
ORDER BY c.cum_sum - r.random ASC
";

using var sqlConnection = new SqlConnection(connectionString);
var command = new SqlCommand { Connection = sqlConnection, CommandText = query };

var teirs = new Dictionary<int, Tier>();

try
{
    sqlConnection.Open();
    for (var i = 0; i < RUN_COUNT; i++)
    {
        var reader = command.ExecuteReader();
        reader.Read();
        var id = int.Parse(reader[0].ToString() ?? throw new InvalidOperationException());
        var name = reader[1].ToString();
        var probability = double.Parse(reader[2].ToString() ?? throw new InvalidOperationException());

        teirs.TryAdd(id, new Tier { Id = id, Name = name!, Probability = probability, Count = 0 });
        teirs[id].Count++;
        reader.Close();   
    }
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}

var result = teirs.Values
    .ToDictionary(t => $"{t.Name}({t.Probability})", t => t.Count)
    .OrderBy(t => t.Key);

Console.WriteLine($"Executed query {RUN_COUNT} times");
PrintHistogram(result.ToList(), ("tier", "count"));

https://stackoverflow.com/a/56140036/5431968
static void PrintHistogram(List<KeyValuePair<string,int>> list, (string, string) headers)
{

    // get the max length of all the words so we can align
    var max = list.Max(x => x.Key.Length);
    var maxCount = list.Max(x => x.Value.ToString().Length);
    var header = $"{headers.Item1.PadLeft(max)}  {headers.Item2.PadRight(maxCount)}";
    for (var i = 0; i < header.Length; i++)
    {
        Console.Write('-');
    }
    Console.WriteLine();

    Console.WriteLine(header);
    foreach (var item in list)
    {
        // right align using PadLeft and max length
        Console.Write(item.Key.PadLeft(max));

        Console.Write($"  {item.Value.ToString().PadRight(maxCount)}  ");
        
        // Write the bars
        // for (var i = 0; i < item.Value; i++)
        //     Console.Write("#");

        Console.WriteLine();
    }
}