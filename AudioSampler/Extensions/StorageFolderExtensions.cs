using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioSampler.Extensions
{
    public static class StorageFolderExtensions
    {
        /// <summary>
        /// расширение костыль чтобы определить существует ли папка
        /// </summary>
        public static async Task<bool> ExistsAsync(this IStorageFolder folder)
        {
            try
            {
                await folder.GetItemsAsync().FirstOrDefaultAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
