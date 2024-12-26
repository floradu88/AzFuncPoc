using Azure.Storage.Blobs;
using Renci.SshNet;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

public static class MonitorSftpFunction
{
    [Function("MonitorSftpFunction")]
    public static void Run(
        [TimerTrigger("0 */1 * * * *")] TimerInfo timer,
        BlobServiceClient blobServiceClient,
        FunctionContext context)
    {
        var logger = context.GetLogger("MonitorSftpFunction");

        logger.LogInformation($"Timer trigger function executed at: {DateTime.Now}");

        // SFTP server details
        string host = "sftpstoragedev2.blob.core.windows.net"; // Blob Storage hostname
        string username = "sftpstoragedev2.files.sftpstoragedev2userrw"; // Correct username format
        string password = "3yls5W2YR0alIO6dod9UbT7L/AkxTQ8Q"; // Access key or password for the user
        string remoteDirectory = "files";

        // Azure Blob Storage container
        string blobContainerName = "files";

        using (var sftp = new SftpClient(host, username, password))
        {
            try
            {
                sftp.Connect();
                logger.LogInformation("Connected to SFTP server");

                var files = sftp.ListDirectory(remoteDirectory);
                foreach (var file in files)
                {
                    if (!file.IsDirectory && !file.IsSymbolicLink)
                    {
                        logger.LogInformation($"Processing file: {file.Name} - Last Modified: {file.LastWriteTime}");

                        // Download file from SFTP
                        using (var fileStream = new MemoryStream())
                        {
                            sftp.DownloadFile(file.FullName, fileStream);
                            fileStream.Position = 0; // Reset the stream position

                            // Upload file to Azure Blob Storage
                            var blobContainer = blobServiceClient.GetBlobContainerClient(blobContainerName);
                            var blobClient = blobContainer.GetBlobClient(file.Name);

                            logger.LogInformation($"Uploading file to Blob Storage: {file.Name}");
                            blobClient.Upload(fileStream, overwrite: true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error processing files from SFTP: {ex.Message}");
            }
            finally
            {
                sftp.Disconnect();
                logger.LogInformation("Disconnected from SFTP server");
            }
        }
    }
}
