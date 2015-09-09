namespace MonoDevelop.Protobuild
{
    public interface IProtobuildModuleTemplate
    {
        string Id { get; }

        string Name { get; }

        string IconId { get; }

        string ImageId { get; }

        string GroupId { get; }

        string Description { get; }

        bool IsFeatured { get; }

        bool IsGeneral { get; }

        bool IsOnline { get; }

        bool IsMisc { get; }
    }
}
