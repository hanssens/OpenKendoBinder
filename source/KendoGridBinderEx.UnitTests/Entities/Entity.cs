namespace OpenKendoBinder.UnitTests.Entities
{
    public abstract class Entity : IEntity
    {
        public virtual long Id { get; set; }
    }
}