namespace Library.API.Helpers
{
    public class AuthorResourceParameters
    {
        private const int MaxPageSize = 20;
        private int _pageSize = 10;

        public int PageNumber { get; set; } = 1;

        public int PageSize
        {
            get
            {
                return _pageSize;
            }

            set
            {
                _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
            }
        }

        public string Genre { get; set; }

        public string SearchQuery { get; set; }

        public string OrderBy { get; set; } = "Name";
    }
}
