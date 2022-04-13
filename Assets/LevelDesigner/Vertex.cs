namespace LevelDesigner
{
    public enum NodeType
    {
        Normal,
        Start,
        Save,
        Boss
    }

    public class Vertex
    {
        public string Name;
        public float Weight;
        public NodeType Type;

        public Vertex(string name, float weight)
        {
            Name = name;
            Weight = weight;
        }

        public override string ToString()
        {
            return $"{Name}({Weight})";
        }
    }
}
