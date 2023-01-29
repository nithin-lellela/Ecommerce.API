namespace Ecommerce.API.Helpers
{
    public class AuthResponseModel
    {
        public AuthResponseModel(ResponseCode responseCode, string responseMessage, object dataSet)
        {
            ResponseCode = responseCode;
            ResponseMessage = responseMessage;
            DataSet = dataSet;
        }

        public ResponseCode ResponseCode { get; set; }
        public string ResponseMessage { get; set; }
        public object DataSet { get; set; }
    }
}

public enum ResponseCode
{
    Ok = 200,
    Created = 201,
    BadRequest = 400,
    UnAuthorized = 401,
    NotFound = 404,
    InternalServerError = 500
}
