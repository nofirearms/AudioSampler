using System;
using System.Collections.Generic;
using System.Text;

namespace AudioSampler.Model
{
    public class Result<T> where T: class
    {

        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public T Parameter { get; set; }

        public static Result<T> Ok(T parameter) => new()
        {
            Success = true,
            Parameter = parameter
        };

        public static Result<T> Fail(string message) => new()
        {
            Success = false,
            ErrorMessage = message
        };

        public static implicit operator Result<T>(T parameter) => Ok(parameter);
    }
}
