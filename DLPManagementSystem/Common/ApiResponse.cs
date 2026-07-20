namespace DLPManagementSystem.Common
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string MessageAr { get; set; } = string.Empty;
        public string MessageEn { get; set; } = string.Empty;
        public T? Data { get; set; }

        public static ApiResponse<T> SuccessResponse(T data, string messageEn = "Success", string messageAr = "تمت العملية بنجاح")
        {
            return new ApiResponse<T>
            {
                Success = true,
                MessageEn = messageEn,
                MessageAr = messageAr,
                Data = data
            };
        }

        public static ApiResponse<T> FailureResponse(string messageEn, string messageAr)
        {
            return new ApiResponse<T>
            {
                Success = false,
                MessageEn = messageEn,
                MessageAr = messageAr,
                Data = default
            };
        }
    }
    
}
