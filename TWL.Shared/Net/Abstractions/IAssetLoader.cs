namespace TWL.Shared.Net.Abstractions;

public interface IAssetLoader
{
    T Load<T>(string asset);
    void UnloadAll();
}