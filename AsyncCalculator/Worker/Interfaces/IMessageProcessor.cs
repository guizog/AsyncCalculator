namespace Worker.Interfaces
{
    public interface IMessageProcessor
    {
        public Task<bool> ProcessAsync(string id);
    }
}
