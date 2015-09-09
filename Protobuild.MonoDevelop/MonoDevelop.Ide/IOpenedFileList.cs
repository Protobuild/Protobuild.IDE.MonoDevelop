using System.Collections.Generic;

namespace MonoDevelop.Ide
{
    public interface IOpenedFileList<T>
    {
        List<T> OpenedFiles { get; set; }
    }
}