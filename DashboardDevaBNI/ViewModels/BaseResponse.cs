namespace DashboardDevaBNI.ViewModels
{
    public class BaseResponse<T>
    {
        public int Code { get; set; } = 1;
        public string Message { get; set; }
        public T Data { get; set; }
    }

}
