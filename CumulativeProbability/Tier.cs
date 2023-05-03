class Tier
{
    public int Id { get; set; }
    public string Name { get; set; }
    public double Probability { get; set; }
    public int Count { get; set; }

    public override string ToString()
    {
        return $"Id={Id}, Name={Name}, Probability={Probability}, Count={Count}";
    }
}