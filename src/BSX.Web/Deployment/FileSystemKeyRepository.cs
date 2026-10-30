using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;

namespace BSX.Web.Deployment;

/// <summary>
/// Minimal file-system XML repository for the Data Protection key ring. The framework's own
/// repository is not part of the public surface in this target framework; this keeps the key ring
/// in an operator-configured persistent directory so cookies survive restarts and can be shared by
/// multiple instances.
/// </summary>
internal sealed class FileSystemKeyRepository : IXmlRepository
{
    private const string FilePrefix = "key-";
    private readonly DirectoryInfo _directory;
    private readonly ILogger<FileSystemKeyRepository> _logger;
    private readonly object _gate = new();

    /// <summary>Initializes a new instance of the <see cref="FileSystemKeyRepository"/> class.</summary>
    /// <param name="directory">The key directory, already validated by the deployment configuration.</param>
    /// <param name="logger">The logger.</param>
    public FileSystemKeyRepository(DirectoryInfo directory, ILogger<FileSystemKeyRepository> logger)
    {
        _directory = directory;
        _logger = logger;
        _directory.Create();
    }

    /// <inheritdoc />
    public IReadOnlyCollection<XElement> GetAllElements()
    {
        lock (_gate)
        {
            var elements = new List<XElement>();

            foreach (FileInfo file in _directory.EnumerateFiles($"{FilePrefix}*.xml"))
            {
                try
                {
                    elements.Add(XElement.Load(file.FullName));
                }
                catch (Exception exception) when (exception is IOException or XmlException)
                {
                    _logger.LogWarning(
                        exception,
                        "Skipping unreadable Data Protection key file {KeyFile}.",
                        file.FullName);
                }
            }

            return elements;
        }
    }

    /// <inheritdoc />
    public void StoreElement(XElement element, string friendlyName)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentException.ThrowIfNullOrWhiteSpace(friendlyName);

        // Friendly names are key identifiers (the key manager uses the `key-{id}` form); never allow
        // path traversal from a repository value.
        string fileName = $"{Path.GetFileName(friendlyName)}.xml";
        string path = Path.Combine(_directory.FullName, fileName);

        lock (_gate)
        {
            element.Save(path);
        }
    }
}
