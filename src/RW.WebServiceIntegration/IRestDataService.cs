namespace RW.WebServiceIntegration
{
    public interface IRestDataService
    {
        bool GetWebResponse<TResponse>(RequestWrapper requestParameters, out TResponse? response)
            where TResponse : class;
    }
}
