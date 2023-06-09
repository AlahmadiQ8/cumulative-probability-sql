# Experimentation with SQL Only Weighted Probabilities

A fun challenge to see if we can implement weighted random drawing using only SQL.

Upon looking online, the closest thing I can find is this answer on Stackoverflow: https://stackoverflow.com/a/33588562/5431968, which uses cumulative sum and random value.

I used the same concept but different query implementation that I found subjectively easier to wrap my head around.

## Requirement

Choose one item from a list, but consider the likelihood of each item as shown in the example table below. Our random selection should take into account these chances.

| item   | probability |
| ------ | ----------- |
| Teir_1 | 0.1         |
| Teir_2 | 0.2         |
| Teir_3 | 0.3         |
| Teir_4 | 0.4         |

NOTE: Sum of all probabilities must sum to 1.

## Via Code

<details>

<summary>For reference, here is how it's done via code</summary>

```csharp
var items = new List<Item>
{
    new() { Id = "Item_1", Probability = 0.1 },
    new() { Id = "Item_2", Probability = 0.2 },
    new() { Id = "Item_3", Probability = 0.3 },
    new() { Id = "Item_4", Probability = 0.4 }
};

const int runs = 1_000_000;
var counts = new Dictionary<string, int>();
for (var i = 0; i < runs; i++)
{
    var item = WeightedFairChance(items);
    counts.TryAdd(item.Id, 0);
    counts[item.Id]++;
}

foreach (var (id, count) in counts)
{
    Console.WriteLine($"{id}  {count}");
}

Item WeightedFairChance(IList<Item> input)
{
    var cumulativeProbability = new List<Item>();
    double sum = 0;
    foreach (var item in input)
    {
        sum += item.Probability;
        cumulativeProbability.Add(new Item {Id = item.Id, Probability = sum});
    }

    var random = new Random().NextDouble();
    var weightedRandomItem = cumulativeProbability
        .OrderBy(item => item.Probability - random)
        .First(item => item.Probability - random >= 0);

    return input.Single(i => i.Id == weightedRandomItem.Id);
}

class Item
{
    public string Id { get; set; }
    public double Probability { get; set; }
}
```
</details>

## Challenge

SQL only, no code. Keep in mind, I'm completely ignoring performance, just whether it's doable or not.

## Approach

The easiest approach is to take the cumulative propability for each item and select a random number evenly distributed `x` where `0 < x < 1`.

Example:

| item | probability | cumulative |
| ---- | ----------- | ---------- |
| A    | 0.2         | 0.2        |
| B    | 0.3         | 0.5        |
| C    | 0.5         | 1          |

If random number `x = 0.45`, then most we should return item `B` because `x` is `0.2 < x <= 0.5`.

```
                           x
|-----|-----|-----|-----|-----|-----|-----|-----|-----|-----|
           0.2               0.5                            1
└──────────┘ └───────────────┘ └───────────────────────────┘
      A              B                        C
```

## Database setup

We'll initialize our database with the following table and values

<details>

<summary>SQL Init</summary>

```sql
CREATE TABLE Tiers
(

    id          BIGINT IDENTITY(1,1) NOT NULL,
    name        varchar(100)  NOT NULL,
    probability decimal(5, 4) NOT NULL
        CONSTRAINT percentage CHECK (probability between 0 and 1)
)
;

INSERT INTO Tiers
    (name, probability)
VALUES ('Teir_1', 0.1),
       ('Teir_2', 0.2),
       ('Teir_3', 0.3),
       ('Teir_4', 0.4)
;
```

</details>

## The Query

Here is the query I came up with.

```sql
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
```

## Testing The Query With 30000 Query Executions

To validate, I ran the above query 30000 times on Azure SQL Server version 12.0.2000.8. See [Program.cs](./CumulativeProbability/Program.cs) for full script.

```
Executed query 30000 times
------------------
       tier  count   actual percent
Teir_1(0.1)  2652    0.0884
Teir_2(0.2)  5188    0.17293333333
Teir_3(0.3)  7933    0.26443333333
Teir_4(0.4)  10685   0.35616666666
```

The results are fairly good, showing that the query functions properly. Although the search could be made simpler or faster. But for a first try, it's quite decent.

