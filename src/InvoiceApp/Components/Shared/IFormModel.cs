namespace InvoiceApp.Components.Shared;

public interface IFormModel<out TDto>
{
    TDto ToDto();
}
