using System;
using System.Collections.Generic;
using System.Text;

namespace ELearningPlatform.Application
{
    public class GenericResponseDTO<T>  where T : class
    {
        public bool IsSuccess { get; set; }
        public T? Data { get; set; }
        public string Message { get; set; } = string.Empty;



        public GenericResponseDTO(bool isSuccess, string message = "")
        {
            IsSuccess = isSuccess;
            Data = null;
            Message = ResolveMessage(isSuccess, message);
        }
        public GenericResponseDTO(bool isSuccess, T? data, string message = "")
        {
            IsSuccess = isSuccess;
            Data = data;
            Message = ResolveMessage(isSuccess, message);
        }

        private static string ResolveMessage(bool isSuccess, string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                return message;
            }

            return isSuccess ? "Request Processed Successfully" : "Request Failed";
        }
    }
}
