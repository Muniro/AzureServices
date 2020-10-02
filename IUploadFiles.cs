using System.Threading.Tasks;

namespace AzureServices
{
    public interface IUploadFiles
    {
        Task UploadFilesAsync(string clientId);
    }
}
