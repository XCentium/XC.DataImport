using Sitecore.Data.Items;
using Sitecore.Resources.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XC.DataImport.Repositories.Repositories
{
  public class BaseDatabaseRepository
  {  
    internal void DetachMedia(MediaItem mediaItem)
    {
      MediaUri mediaUri = MediaUri.Parse(mediaItem);
      Media media = MediaManager.GetMedia(mediaUri);
      if (media != null)
      {
        media.ReleaseStream();
      }
    }
   
  }
}
