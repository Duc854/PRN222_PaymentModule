
---

# PaymentModule(Three-Layer Architecture)ASP.NET Core MVC – Project Structure & Basics

## 1. PaymentModule.Web(MVC)

* **Model**: dữ liệu, logic.
* **View**: giao diện (cshtml).
* **Controller**: trung gian, nhận request → xử lý → response.
* ****Infrastructure**: Cấu hình DI và các external service ở đây

---

## 2. PaymentModule.Business (Dùng để viết các logic liên quan đến sử lý nghiệp vụ như tính hóa đơn tính tổng đơn hàng áp mã giảm giá...)

* **Abstraction**: dùng để chứa các interface của business service của hệ thống
* **Services**: dùng để chứa các implement của interface service và chứa các services bên ngoài vd như service mail của google
* **Dtos**: dùng để nhận data từ tầng presen (MVC)
* **Mappings**: dùng để chuyển data từ Dtos sang Entities đại diện cho các tbl trong DB ví dụ khi đăng kí User yêu cầu cf password thì dto sẽ nhận cả 2 loại password và cfpassword sau đó dùng services để xác
  nhận rồi mapping thành user chỉ gồm username và password để làm data cho DB
* **Exception**: chứa thông tin mã lỗi và thông báo lỗi khi xảy ra vấn đề với logic nghiệp vụ ví dụ đơn hàng bị âm tiền

---
## 3. PaymentModule.Data (Dùng để thực hiện thao tác vơi DB đảm bảo SOLID service sẽ ko vừa thực hiện nghiệp vụ vừa thao tác với DB)

* **Abstraction**: dùng để chứa các interface của repository (class thao tác với DB có thể tham khảo Repository Design pattern trong C#) của hệ thống
* **Repositories**: dùng để chứa các implement của repository
* **Entities**: chứa các class đại diện cho các table trong DB và chứa DBContext (class cấu hình mối quan hệ, mô tả khóa của bảng, thuộc tính và nhiều đặc điểm khác của DB)

---

## 4. Dependency Injection (DI)

Đăng ký trong `Program.cs`:
```csharp
\\\Thêm toàn bộ các DI Khi thay đổi hay thêm mới service, chỉ sửa trong extension, không ảnh hưởng file Program.cs.
builder.Services.AddInfrastructure(builder.Configuration);

```csharp Dùng Addcope, AddDbContext,AddSingleton... tùy vào mục đích sử dụng của DI
 //Add SqlServer
 var connectionString = configuration.GetConnectionString("SQLServerConnection");
 services.AddDbContext<CloneEbayDbContext>(options => options.UseSqlServer(connectionString));

 //Add Services

 //Add Repositories
```

Controller chỉ inject **interface**:

```csharp
public EntityXController(IEntityXService service) { ... }
```
Service chỉ inject **interface**:

```csharp
public EntityXService(IEntityXRepository EntityXRepository) { ... }
```
---

## 5. Workflow

1. **Controller** là tâng presentaion(MVC) nhận request và truyền dữ liệu về service layer(PaymentModule.Business) qua DI
2. **DTOs** nhận dữ liệu request từ tâng presentaion(MVC)
3. **PaymentModule.Business.Abstraction** (interface) khai báo cho Controller (tầng presentation) biết về service
4. **Services** nhận Dtos, thao tác với dữ liệu của Dtos để sử lý nghiệp vụ (như tính đơn hàng, kiểm tra đúng sai về giá hay mật khẩu confirm)
5. **Mappings** map data từ Dtos qua Entities
6. **PaymentModule.Data.Abstraction** (interface) khai báo cho Services (tầng Business) biết về Repository (class sẽ thao tác trực tiếp với DB như Select, Add, Delete...)
7. **Entities** Nhận data dc mapping từ tầng business
9. **Repositories** nhận Entities và thực hiện tháo tác với DB tùy vào hàm dc gọi trong Service

---


