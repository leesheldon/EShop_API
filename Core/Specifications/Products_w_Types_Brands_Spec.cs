using Core.Entities;

namespace Core.Specifications
{
    public class Products_w_Types_Brands_Spec : BaseSpecification<Product>
    {
        public Products_w_Types_Brands_Spec(ProductSpecParams productParams) 
            : base(x => 
                (string.IsNullOrEmpty(productParams.Search) || x.Name.ToLower().Contains(productParams.Search)) &&
                (!productParams.BrandId.HasValue || x.ProductBrandId == productParams.BrandId) &&
                (!productParams.TypeId.HasValue || x.ProductTypeId == productParams.TypeId)
            )
        {
            AddInclude(x => x.ProductType);
            AddInclude(x => x.ProductBrand);
            AddOrderBy(x => x.Name);
            AddInclude(x => x.Photos);
            ApplyPaging(productParams.PageSize * (productParams.PageIndex - 1), productParams.PageSize);

            if (!string.IsNullOrEmpty(productParams.Sort))
            {
                switch (productParams.Sort)
                {
                    case "idAsc":
                        AddOrderBy(p => p.Id);
                        break;
                    case "idDesc":
                        AddOrderByDescending(p => p.Id);
                        break;
                    case "priceAsc":
                        AddOrderBy(p => p.Price);
                        break;
                    case "priceDesc":
                        AddOrderByDescending(p => p.Price);
                        break;
                    case "nameAsc":
                        AddOrderBy(p => p.Name);
                        break;
                    case "nameDesc":
                        AddOrderByDescending(p => p.Name);
                        break;
                    case "brandAsc":
                        AddOrderBy(p => p.ProductBrand.Name);
                        break;
                    case "brandDesc":
                        AddOrderByDescending(p => p.ProductBrand.Name);
                        break;
                    case "typeAsc":
                        AddOrderBy(p => p.ProductType.Name);
                        break;
                    case "typeDesc":
                        AddOrderByDescending(p => p.ProductType.Name);
                        break;
                    default:
                        AddOrderBy(n => n.Name);
                        break;
                }
            }
        }

        public Products_w_Types_Brands_Spec(int id) 
            : base(x => x.Id == id)
        {
            AddInclude(x => x.ProductType);
            AddInclude(x => x.ProductBrand);
            AddInclude(x => x.Photos);
        }

    }
}
