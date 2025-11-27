using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;

namespace DashboardDevaBNI.ViewModels
{
    public class ResultStatus
    {
        public string StatusCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string ValueGeneratorOtp { get; set; } = string.Empty;
    }
    public class ResultStatus<T> : ResultStatus where T : class
    {
        public T Data { get; set; }
    }

    public class ListResultStatus<T> : ResultStatus where T : class
    {
        public List<T> Data { get; set; }
        public int Count { get; set; }
    }

    public class ResultStatusInt
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }
    }

    public class ResultStatusDataString<T>
    {
        public string StatusCode { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }

    }

    public class ResultStatusDataInt<T>
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }
        [JsonProperty("@odata.count")]
        public int ODataCount { get; set; }
        public T Data { get; set; }
        public List<T> Value { get; set; }

    }

    public class ResultStatusDataNod
    {
        public string Key { get; set; }
        public string Id { get; set; }
        public string Status { get; set; }

    }

    public class ResultStatusDataNop
    {
        public string Key { get; set; }
        public string Id { get; set; }
        public string Status { get; set; }

    }
}
