namespace Sevenisko.IceBot
{
	public class AudioFile
	{
		private string CFileName;

		private string CTitle;

		private bool CIsNetwork;

		private bool CIsDownloaded;

        private string CLink;

		public string FileName
		{
			get
			{
				return CFileName;
			}
			set
			{
				CFileName = value;
			}
		}

        public string Link
        {
            get
            {
                return CLink;
            }
            set
            {
                CLink = value;
            }
        }

		public string Title
		{
			get
			{
				return CTitle;
			}
			set
			{
				CTitle = value;
			}
		}

		public bool IsNetwork
		{
			get
			{
				return CIsNetwork;
			}
			set
			{
				CIsNetwork = value;
			}
		}

		public bool IsDownloaded
		{
			get
			{
				return CIsDownloaded;
			}
			set
			{
				CIsDownloaded = value;
			}
		}

		public AudioFile()
		{
			FileName = "";
			Title = "";
            Link = "";
			IsNetwork = true;
			IsDownloaded = false;
		}

		public override string ToString()
		{
			return Title;
		}
	}
}
