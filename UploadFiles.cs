using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AzureServices
{
    public class UploadFiles : IUploadFiles
    {
        public async Task UploadFilesAsync(string clientId)
        {
            // Get Azure containers to upload files to.
            var container = await GetClientContainerAsync(clientId);

            // path to the directory to upload
            var currentdir = System.IO.Directory.GetCurrentDirectory();
            string uploadPath = String.Concat(clientId,"\\",currentdir , "\\upload");
                    

            // benchmark the speed
            Stopwatch time = Stopwatch.StartNew();


            try
            {
                Console.WriteLine("Iterating in directory: {0}", uploadPath);
                int count = 0;
                int max_thread_left = 100; //Max thread to use during the process
                int completed_count = 0;



                // Define the BlobRequestOptions on the upload.
                // This includes defining an exponential retry policy to ensure that failed connections are retried with a backoff policy. As multiple large files are being uploaded
                // large block sizes this can cause an issue if an exponential retry policy is not defined.  Additionally parallel operations are enabled with a thread count of 8
                // This should be multiple of the number of cores that the machine has. Lastly MD5 hash validation is disabled for this example, this improves the upload speed.
                BlobRequestOptions options = new BlobRequestOptions
                {
                    ParallelOperationThreadCount = 8,    //The setting breaks the blob into blocks when uploading. For highest performance, this value should be eight times the number of cores.
                    DisableContentMD5Validation = true, //The files are uploaded in 100-mb blocks, this configuration provides better performance
                    StoreBlobContentMD5 = false         //This property determines if an MD5 hash is calculated and stored with the file. False makes it faster
                };


                // Create a new instance of the SemaphoreSlim class to define the number of threads to use in the application.
                SemaphoreSlim sem = new SemaphoreSlim(max_thread_left, max_thread_left);

                List<Task> tasks = new List<Task>();
                Console.WriteLine("Found {0} file(s)", Directory.GetFiles(uploadPath).Count());

                // Iterate through the files
                foreach (string path in Directory.GetFiles(uploadPath))
                {
                    
                    string fileName = Path.GetFileName(path);
                    Console.WriteLine("Uploading {0} to container {1}.", path, container.Name);
                    CloudBlockBlob blockBlob = container.GetBlockBlobReference(fileName);

                    // Set block size to 100MB.
                    blockBlob.StreamWriteSizeInBytes = 100 * 1024 * 1024;
                    await sem.WaitAsync();

                    // Create tasks for each file that is uploaded. This is added to a collection that executes them all asyncronously.  
                    tasks.Add(blockBlob.UploadFromFileAsync(path, null, options, null).ContinueWith((t) =>
                    {
                        sem.Release();
                        Interlocked.Increment(ref completed_count);
                    }));
                    count++;
                }

                // Creates an asynchronous task that completes when all the uploads complete.
                await Task.WhenAll(tasks);

                time.Stop();

                Console.WriteLine("Upload has been completed in {0} seconds. Press any key to continue", time.Elapsed.TotalSeconds.ToString());

                Console.ReadLine();
            }
            catch (DirectoryNotFoundException ex)
            {
                Console.WriteLine("Error parsing files in the directory: {0}", ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

           

        }

        private async Task<CloudBlobContainer> GetClientContainerAsync(string clientId)
        {
            throw new NotImplementedException();
        }

    }
}
