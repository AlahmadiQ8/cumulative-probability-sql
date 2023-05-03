# Experimentation with SQL Only Weighted Probabilities

A fun challenge to see if we can implement weighted random drawing using only SQL.

## Requirement

Choose one item from a list, but consider the likelihood of each item as shown in the example table below. Our random selection should take into account these chances.

| item   | probability |
| ------ | ----------- |
| Teir_1 | 0.1         |
| Teir_2 | 0.2         |
| Teir_3 | 0.3         |
| Teir_4 | 0.4         |

NOTE: Sum of all probabilities must sum to 1.


## Challenge

Is it possible to do this using only SQL and not code? How dependable is it? Let's find out what we can do. Keep in mint, I'm completely ignoring performance, just whether it's doable or not.

## Approach

The easiest approach is to take the cumulative propability for each item and select a random number `x` where `0 < x < 1`.

Example:

| item | probability | cumulative |
| ---- | ----------- | ---------- |
| A    | 0.2         | 0.2        |
| B    | 0.3         | 0.5        |
| C    | 0.5         | 1          |

If random number `x = 0.45`, then most we should return item `B`.

```
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
-- https://stackoverflow.com/a/2762376/5431968
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

## The ultimate Query

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

I believe we received fairly good results, showing that the search functions properly. Although the search could be made simpler or faster. But for a first try, it's quite decent.

