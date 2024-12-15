using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Renci.SshNet;

namespace MyFunctionApp;

public static class MonitorSftp
{
    [FunctionName("MonitorSftp")]
    public static void Run([TimerTrigger("0 */1 * * * *")] TimerInfo timer, ILogger log)
    {
        log.LogInformation($"Timer trigger function executed at: {DateTime.Now}");

        string host = "sftpstoragedev2.blob.core.windows.net";
        string username = "sftpstoragedev2userrw";
        string password = "3yls5W2YR0alIO6dod9UbT7L/AkxTQ8Q";
        string remoteDirectory = "/files";

        using (var sftp = new SftpClient(host, username, password))
        {
            try
            {
                sftp.Connect();
                log.LogInformation("Connected to SFTP server");

                var files = sftp.ListDirectory(remoteDirectory);
                foreach (var file in files)
                {
                    if (!file.IsDirectory && !file.IsSymbolicLink)
                    {
                        log.LogInformation($"File found: {file.Name} - {file.LastWriteTime}");

                        // Process new files logic here
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError($"Error connecting to SFTP: {ex.Message}");
            }
            finally
            {
                sftp.Disconnect();
            }
        }
    }
}