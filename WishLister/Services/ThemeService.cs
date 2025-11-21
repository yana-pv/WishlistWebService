using WishLister.Models;
using WishLister.Repository.Interfaces;

namespace WishLister.Services;
public class ThemeService
{
    private readonly IThemeRepository _themeRepository;

    public ThemeService(IThemeRepository themeRepository)
    {
        _themeRepository = themeRepository;
    }


    public async Task<List<Theme>> GetAllThemesAsync()
    {
        return await _themeRepository.GetAllAsync();
    }


    public async Task<Theme?> GetThemeAsync(int id)
    {
        return await _themeRepository.GetByIdAsync(id);
    }
}