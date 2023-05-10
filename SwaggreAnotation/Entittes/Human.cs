namespace SwaggreAnotation.Entittes
{
    public interface IEntity
    {

    }
    public class Human : IEntity
    {

        public string Name { get; set; }
        public string Description { get; set; }
        public Human() { }

        public Human(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}
