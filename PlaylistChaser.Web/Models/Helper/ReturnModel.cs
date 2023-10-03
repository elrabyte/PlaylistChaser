namespace PlaylistChaser.Web.Models
{
    public class ReturnModel
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public ReturnModel()
        {
            Success = true;
        }
        public ReturnModel(string errorMessage)
        {
            Success = false;
            ErrorMessage = errorMessage;
        }
        public ReturnModel(bool success, string errorMessage)
        {
            Success = success;
            ErrorMessage = errorMessage;
        }
    }
}