# **E-COMMERCE WEB APPLICATION - SRS DOCUMENT**

## **1. INTRODUCTION & PURPOSE**

### 1.1 Project Overview
This is a full-featured ASP.NET Core MVC e-commerce application designed for online retail operations. The system facilitates product catalog management, customer shopping experiences, order processing, and payment integration with Vietnamese payment gateways (VNPay and Momo).

### 1.2 Target Users
- **End Customers**: Browse products, build shopping carts, place orders, track purchases
- **Administrators**: Manage product catalog, categories, orders, users, and promotional coupons
- **System**: Payment gateway integration with real-time transaction handling

### 1.3 Technology Stack
- **Framework**: ASP.NET Core (net8.0)
- **Database**: SQL Server (LocalDB)
- **ORM**: Entity Framework Core
- **Authentication**: ASP.NET Core Identity
- **Architecture**: MVC with Areas (Admin panel separation)
- **API**: RESTful API endpoints for products

---

## **2. FUNCTIONAL REQUIREMENTS**

### **2.1 USER MANAGEMENT & AUTHENTICATION**

#### FR-2.1.1 User Registration
- Users can create accounts with email-based registration
- **Required fields**: Email, Password, Full Name, Address
- **Password policy**:
  - Minimum 6 characters
  - At least one uppercase letter, one lowercase letter, one digit
  - No special characters required
- **Validation**: Email must be unique (unique constraint enforced)
- **Default role assignment**: All new users assigned "Customer" role automatically

#### FR-2.1.2 User Login
- Email-based login only (email used as username)
- Password authentication via ASP.NET Core Identity
- Password incorrect: Account lockout on failure (via failLockoutOnFailure: true)
- **Login flow**:
  - Validate credentials against Identity database
  - Set authentication cookie (7-day sliding expiration)
  - Support optional "Remember Me" functionality
  - Support return URL redirection post-login
- **Admin login redirect**: Admin users automatically redirected to Admin panel after login

#### FR-2.1.3 Session Management
- Cookie-based sessions (distributed memory cache)
- Session timeout: 2 hours idle time
- HttpOnly cookies enabled for security
- Session used for storing applied coupon codes during checkout

#### FR-2.1.4 User Profile Management
- **View profile**: Display current email, full name, phone number, address
- **Edit profile**: Update full name, phone number, and address
- **Read-only email**: Email cannot be changed post-registration

#### FR-2.1.5 Logout
- Secure sign-out from all devices
- Redirect to home page post-logout

### **2.2 PRODUCT MANAGEMENT**

#### FR-2.2.1 Product Catalog (Customer View)
- **Browse all products** with pagination (12 items per page)
- **Product details display**:
  - Product name, description (up to 2000 chars)
  - Original price and discounted price
  - Stock availability
  - Product category
  - Product image
- **Filtering capabilities**:
  - Search by product name or description (full-text)
  - Filter by category
  - Price range filtering (min/max)
- **Sorting options**:
  - Newest first (default, by product ID descending)
  - Price ascending
  - Price descending
- **Pagination support**: Query string parameters preserved across pages

#### FR-2.2.2 Product Details Page
- Display complete product information
- Show effective price calculation (based on discount percentage)
- Display current stock levels
- One-click "Add to Cart" functionality

#### FR-2.2.3 Product Discount System
- Products can have **discount percentage** (0-100%)
- **Effective price calculation**: `Price × (100 - DiscountPercent) / 100`
- Prices rounded to nearest integer (VND currency)
- Discount percentage displayed on product cards/listings

#### FR-2.2.4 Admin Product Management
- **List products**: Search by name/description, view all products
- **Create products**:
  - Product name (max 200 chars), description, price, stock, discount %
  - Assign to category
  - Upload product image (file handling with wwwroot/uploads storage)
  - Default image: placeholder-product.svg if no image uploaded
  - Discount percentage validation (0-100)
- **Edit products**: Update all fields including image replacement
- **Delete products**: Soft-delete capability (remove stock)
- **Image management**: Server-side image storage with configurable paths

### **2.3 PRODUCT CATEGORIES**

#### FR-2.3.1 Category Management (Admin Only)
- **Create categories**: Name (max 120 chars), optional description (max 500 chars)
- **Edit categories**: Update name and description
- **Delete categories**:
  - Allow deletion only if no products assigned
  - Prevent data orphaning with validation check
  - Error message: "Cannot delete categories with products"
- **List categories**: Alphabetically sorted by name

#### FR-2.3.2 Category Display (Customer View)
- Categories displayed in navigation/menu
- Products can be filtered by selected category
- Categories linked in product listing view

### **2.4 SHOPPING CART**

#### FR-2.4.1 Cart Operations (Authenticated Users Only)
- **Add to cart**:
  - Check stock availability before adding
  - Support quantity parameter (default 1)
  - Merge with existing cart items (increment quantity)
  - Validate total quantity doesn't exceed stock
  - Error handling for out-of-stock conditions
- **View cart**:
  - Display all cart items with product details
  - Show quantity and line totals (effective_price × quantity)
  - Display empty cart message
- **Update quantity**: Modify item quantity with stock validation
- **Remove from cart**: Delete cart line item
- **Return URL support**: Safe redirect using local URL validation

#### FR-2.4.2 Cart Persistence
- Cart stored in database (CartItem entity)
- Linked to authenticated user (UserId)
- **Unique constraint**: Each user can have only one cart line per product
- Cart persists across sessions
- **Cart count component**: View component displays number of items in cart

### **2.5 ORDER MANAGEMENT**

#### FR-2.5.1 Order Lifecycle
- **Order statuses**:
  - `Pending`: Initial state
  - `Processing`: Order being prepared
  - `Shipped`: In transit
  - `Delivered`: Customer received
  - `Cancelled`: Order cancelled
  - `AwaitingPayment`: Online payment expected (for gateway orders)

- **Payment statuses**:
  - `None`: No payment required (COD)
  - `Pending`: Awaiting payment (online checkout)
  - `Paid`: Successfully paid
  - `Failed`: Payment declined

#### FR-2.5.2 Checkout Process
- **Pre-checkout validation**:
  - Cart must not be empty
  - All products must have available stock
  - Real-time stock verification

- **Checkout information**:
  - Pre-populate with user profile data (full name, phone, address)
  - Allow customer to modify shipping details for this order
  - Shipping fields: ShippingName, ShippingPhone (max 20 chars), ShippingAddress (max 500 chars)

- **Order creation with transaction**:
  - Database transaction ensures consistency
  - Calculate subtotal from cart items (using effective prices)
  - Apply coupon discount if provided
  - Calculate final total amount
  - Create Order with related OrderDetails (one per cart line)
  - Update product stock (decrement by order quantity)
  - Clear shopping cart after order creation

- **Order data storage**:
  - OrderDate: UTC timestamp
  - SubtotalAmount: Sum of discounted product prices
  - CouponDiscountAmount: Discount value from coupon
  - TotalAmount: Final price to pay
  - PaymentMethod: Selected method (COD, VNPay, Momo)
  - PaymentStatus: Initial status based on method
  - AppliedCouponCode: Normalized coupon code for reference
  - GatewayTxnRef: Transaction reference for payment gateway callback handling

#### FR-2.5.3 Payment Method Selection
- **Cash on Delivery (COD)**:
  - No pre-payment required
  - Order marked as "Pending" with PaymentStatus "None"
  - Customer pays upon delivery

- **VNPay Online Payment**:
  - Requires VNPay gateway configuration (TmnCode, HashSecret, ReturnUrl)
  - Order marked as "AwaitingPayment"
  - Redirect to VNPay payment page
  - Amount in VND (multiplied by 100 per VNPay spec)
  - Transaction reference: `{orderId}_{timestamp}`
  - Payment timeout: 15 minutes
  - HMAC-SHA512 signature validation

- **Momo Online Payment**:
  - Requires Momo configuration (PartnerCode, AccessKey, SecretKey, ReturnUrl, NotifyUrl)
  - Wallet capture method
  - Order marked as "AwaitingPayment"
  - Request ID: `{orderTxnId}{timestamp}`
  - Amount in VND
  - Signature: HMAC-SHA256

#### FR-2.5.4 Admin Order Management
- **View all orders**: 
  - Paginated list
  - Search by email, order ID, phone number
  - Sorted by OrderDate descending
- **Order details**: 
  - View complete order info, shipping details, items ordered
  - View payment status and method
  - See associated coupon code if applied
- **Order status updates**: Admin can change order status (Pending, Processing, Shipped, Delivered, Cancelled)
- **Reports capability**: List foundation for future reporting

#### FR-2.5.5 Customer Order History
- **View my orders**:
  - Display only orders belonging to logged-in user
  - Sorted by OrderDate descending
  - Show order status and payment information
  - Access order details
- **Order details view**: See all order details, confirmation information

### **2.6 COUPON & DISCOUNT SYSTEM**

#### FR-2.6.1 Coupon Types
- **Two discount types**:
  1. **Percentage-based**: Discount calculated as percentage of subtotal (0-100%)
     - Optional maximum discount cap (e.g., max 500,000 VND)
  2. **Fixed amount**: Fixed VND amount deducted from subtotal

#### FR-2.6.2 Coupon Validation Logic
During checkout, coupon validation checks:
- Code exists in system
- Coupon is active (IsActive = true)
- Current UTC time within StartDateUtc to EndDateUtc
- UsageLimit not exceeded (UsedCount < UsageLimit)
- Minimum order value requirement met (if specified)
- Order subtotal sufficient to apply discount

Validation errors provide specific messages (e.g., "Expired", "Usage limit reached", "Minimum order not met")

#### FR-2.6.3 Coupon Application
- **Session storage**: Applied coupon stored in session (key: "checkout_coupon_code")
- **Apply during checkout**: 
  - Validate coupon code
  - Recalculate total with discount
  - Display discount amount breakdown

- **Remove coupon**: Clear session and recalculate total
- **Discount calculation**:
  - Percentage type: `discount = subtotal × percentage / 100` (capped by MaxDiscountAmount if set)
  - Fixed type: `discount = fixed_amount` (cannot exceed subtotal)
  - Final: `discount = min(calculated_discount, subtotal)`

#### FR-2.6.4 Admin Coupon Management
- **Create coupons**:
  - Code (max 40 chars, stored uppercase)
  - Discount type (Percent or Fixed Amount)
  - Value parameter
  - Optional min order value
  - Optional max discount cap (for percentage discounts)
  - Start and end dates (Vietnam timezone converted to UTC)
  - Usage limit (optional, null = unlimited)
  - Active/inactive toggle
- **Edit coupons**: Modify all fields, update UsedCount tracking
- **List coupons**: View all coupons with used count and active status
- **Delete coupons**: Remove coupon from system
- **Date handling**: Vietnam timezone conversion utility for local date display

### **2.7 PAYMENT GATEWAY INTEGRATION**

#### FR-2.7.1 VNPay Integration
**Configuration**:
- TmnCode: Terminal code from VNPay
- HashSecret: Security key for HMAC-SHA512 signing
- ReturnUrl: Absolute URL for return after payment (fallback: dynamic)
- IpnUrl: Optional webhook URL for server-to-server callbacks
- PaymentUrl: Sandbox or production endpoint

**Payment process**:
1. Generate secure payment URL with parameters:
   - Amount in VND × 100
   - Order information: "Thanh toan don hang #{orderId}"
   - Locale: Vietnamese (vn)
   - Expiration: 15 minutes
   - OrderType: "other"
   - CurrCode: VND

2. Client IP address captured for fraud detection
3. HMAC-SHA512 signature with lowercase hex encoding

**Return handling**:
- Two callback paths:
  1. `/Payment/VnpayReturn`: User return URL (also confirms payment)
  2. `/Payment/VnpayIpn`: Server-to-server webhook

**Signature validation**:
- Extract vnp_SecureHash parameter
- Reconstruct signature from sorted parameters (excluding vnp_SecureHash and vnp_SecureHashType)
- Case-insensitive comparison

**Payment confirmation**:
- Check vnp_ResponseCode == "00" (success)
- Check vnp_TransactionStatus == "00" (completed)
- Update order PaymentStatus to "Paid"
- Log failed transactions (PaymentStatus: "Failed")

#### FR-2.7.2 Momo Integration
**Configuration**:
- PartnerCode: Merchant identifier
- AccessKey: Public key for request signing
- SecretKey: Private key for HMAC-SHA256
- ReturnUrl: Absolute URL for return (fallback: dynamic)
- NotifyUrl: Webhook for payment confirmation (fallback: dynamic)
- Endpoint: Test or production API URL

**Payment creation**:
1. Generate secure payment URL via HTTP POST request (not redirect)
2. Request parameters:
   - Amount in VND (integer, no decimals)
   - orderId: Transaction ID (e.g., order ID with timestamp)
   - orderInfo: "Thanh toan don hang #{orderId}"
   - requestId: Unique identifier per request
   - requestType: "captureWallet" (pre-approved capture)
   - lang: "vi" (Vietnamese)

3. HMAC-SHA256 signature calculation on sorted canonical string
4. Response: Returns payUrl for QR code display or redirect

**Return handling**:
- Customer redirected from Momo after payment decision
- Validate return signature
- Extract orderId, resultCode (0 = success, others = failure)
- Update order status accordingly

**Webhook notification** (optional):
- Server-to-server notification for payment confirmation
- Validate webhook signature
- Update order status independent of user return

#### FR-2.7.3 Payment Failure Handling
- **Validation errors**: Signature validation failures return error pages
- **Transaction rollback**: Database transaction ensures consistency
- **Error logging**: Failed callbacks logged with exception details
- **User feedback**: Temporary data flash messages ("Payment failed", "Payment successful")

### **2.8 REST API ENDPOINTS**

#### FR-2.8.1 Products API
- **GET /api/products**:
  - Query parameters: `q` (search term), `categoryId` (filter)
  - Returns: List of products with calculated effective prices
  - Response includes: Id, Name, Description, Price, OriginalPrice, EffectivePrice, DiscountPercent, Stock, ImageUrl, Category
  - No authentication required (public API)

- **GET /api/products/{id}**:
  - Returns: Single product details
  - Same response fields as list
  - Category association included
  - Returns 404 if product not found

#### FR-2.8.2 API Response Format
- JSON format
- Informative field names (OriginalPrice, EffectivePrice for clarity)
- Price calculations performed server-side
- AsNoTracking queries for performance (read-only API)

### **2.9 ADMIN PANEL**

#### FR-2.9.1 Admin Area Structure
- **Separate area**: "/Admin" route prefix
- **Access control**: [Authorize(Roles = "Admin")] on all controllers
- **Admin home**: Redirected to after login for admin accounts
- **Features**: Products, Categories, Orders, Coupons, Users

#### FR-2.9.2 Admin User Management
- **List users**:
  - Display all registered users sorted by email
  - Show user email, full name, assigned roles
  - Read-only view (no edit capability in current version)

#### FR-2.9.3 Admin Dashboard Foundation
- Admin area provides controllers for:
  - Products CRUD
  - Categories CRUD
  - Orders viewing and status management
  - Coupons CRUD
  - Users listing

### **2.10 CONTACT FORM**

#### FR-2.10.1 Contact Page
- Simple contact form (not authenticated)
- **Fields**: Name, Email, Phone, Message (implied from ContactViewModel)
- **Submission**: Displays thank you message after submission
- **Current behavior**: No actual email delivery (UI placeholder for future implementation)

---

## **3. NON-FUNCTIONAL REQUIREMENTS**

### **3.1 PERFORMANCE REQUIREMENTS**

- **Page load time**: < 2 seconds for standard products page (12 items)
- **Database queries**: Eager loading used (Include()) to prevent N+1 queries
- **Pagination**: 12 items per page to optimize initial load
- **Caching**:
  - Distributed memory cache for session management
  - AsNoTracking() for read-only queries (API endpoints)
  - No explicit caching layer (opportunity for future enhancement)
- **Async operations**: All I/O operations use async/await (Task-based)
- **API response time**: < 200ms for /api/products endpoints

### **3.2 SECURITY REQUIREMENTS**

#### FR-3.2.1 Authentication & Authorization
- **ASP.NET Core Identity**: Built-in user authentication
- **Password policy**:
  - Strong password enforcement (uppercase, lowercase, digit, minimum 6 chars)
  - No plaintext storage (hashed with PBKDF2)
- **Session security**:
  - HttpOnly cookies (prevents JavaScript access)
  - Secure flag (HTTPS only in production)
  - Sliding expiration (7 days)
- **Login attempts**: Account lockout on repeated failed attempts
- **Cookie authentication**: Automatic logout on browser close (no persistent cookies option tested)

#### FR-3.2.2 Authorization
- **Role-based access control (RBAC)**:
  - Admin role: Access to admin controllers
  - Customer role: Access to customer features
  - Anonymous: Access to public browsing
- **Attribute-based authorization**: [Authorize(Roles = "Admin")] on protected actions
- **Payment callback bypass**: [AllowAnonymous] on PaymentController for gateway callbacks

#### FR-3.2.3 CSRF Protection
- **Token validation**: [ValidateAntiForgeryToken] on all POST/PUT/DELETE actions
- **Token generation**: Razor views include @Html.AntiForgeryToken()
- **Scope**: Customer and admin forms protected

#### FR-3.2.4 Payment Security
- **Signature validation**:
  - VNPay: HMAC-SHA512 signatures validated
  - Momo: HMAC-SHA256 signatures validated
  - Prevents tampering and spoofed payments

- **Transaction reference matching**:
  - Payment callbacks matched against stored GatewayTxnRef
  - Prevents replay attacks

- **TLS/HTTPS**:
  - Enforced in production (UseHttpsRedirection())
  - HSTS headers in production environment

- **Secrets management**:
  - Payment gateway secrets in appsettings.json (opportunity for Key Vault migration)
  - Currently not using Azure Key Vault

#### FR-3.2.5 Data Protection
- **SQL injection prevention**: EF Core parameterized queries
- **XSS protection**: Model validation + ASP.NET Core default HTML encoding
- **URL validation**: Local URL validation on redirects (SafeRedirect pattern)
- **Input validation**:
  - ModelState validation on all forms
  - Data annotations on entities
  - Quantity range validation (1-9999)

#### FR-3.2.6 Sensitive Data Handling
- **Cart access**: Tied to UserId (claims-based)
- **Order access**: Filtered by UserId to prevent customer accessing others' orders
- **Admin operations**: Checked against Admin role
- **PII storage**: Full name, address, phone number stored (encrypted at rest recommended)

### **3.3 USABILITY REQUIREMENTS**

#### FR-3.3.1 User Interface
- **Responsive design**: Views support various screen sizes (Bootstrap likely used)
- **Navigation**: Category menu, main navigation, cart display
- **Feedback**: TempData messages for success/error notifications
- **Pagination**: Clear pagination controls with current page indication
- **Search**: Easy-to-use search and filter interfaces

#### FR-3.3.2 Language & Localization
- **Default language**: Vietnamese (comments and strings in Vietnamese)
- **UI text**: Primarily in Vietnamese
- **Date formatting**: Vietnam timezone support (VietnamTime utility)
- **Currency**: Vietnamese Dong (VND) with proper formatting

#### FR-3.3.3 Accessibility
- **Form validation**: Clear error messages on ModelState errors
- **Helpful text**: Coupon error messages explain why application failed
- **Navigation**: Categories menu accessible from product pages
- **Session timeout**: 2-hour session allows adequate interaction time

### **3.4 RELIABILITY & DATA INTEGRITY**

#### FR-3.4.1 Database Transactions
- **Checkout transaction**: Wrapped in database transaction
  - Ensures atomicity: Order created and cart cleared atomically
  - Prevents orphaned orders if payment method fails

- **Payment callback transaction**: Transaction ensures:
  - Order status updated atomically
  - Prevents race conditions from multiple payment notifications

- **Rollback on error**: Exceptions trigger automatic rollback

#### FR-3.4.2 Data Validation
- **Product stock validation**:
  - Checked at cart add time
  - Re-checked at checkout to prevent overselling
  - Prevents negative stock

- **Coupon validation**: Advanced validation prevents invalid discount application

- **Category deletion guard**: Prevents deletion if products exist

#### FR-3.4.3 Error Handling
- **Try-catch blocks**: Payment callbacks wrap logic in try-catch
- **Error logging**: ILogger<T> injected in PaymentController for failed transactions
- **User-friendly messages**: Generic error pages in production, detailed in development
- **Exception handling middleware**: UseExceptionHandler configured for /Home/Error

#### FR-3.4.4 Backup & Recovery
- **Database**: SQL Server local/cloud backup strategy not specified (requirement)
- **Migrations**: Entity Framework migrations version controlled

### **3.5 SCALABILITY REQUIREMENTS**

- **Stateless architecture**: MVC actions can be scaled horizontally
- **Database**: SQL Server supports connection pooling and replication
- **Session storage**: Distributed memory cache (can migrate to Redis)
- **Static files**: CDN-ready (wwwroot folder isolated)
- **API design**: RESTful endpoints support caching and scaling
- **Async operations**: Non-blocking I/O allows more concurrent users

### **3.6 MAINTAINABILITY & CODE QUALITY**

- **Architecture**:
  - Clear separation: Controllers, Services, Data, Models
  - Dependency injection used throughout
  - Repository pattern not explicitly used (direct context injection)

- **Naming conventions**:
  - PascalCase for public members
  - Camel case for local variables
  - Descriptive method names

- **Code organization**:
  - Models grouped by entity
  - ViewModels separate from domain models
  - Services for business logic

- **Documentation**:
  - XML comments on key methods (especially Payment services)
  - Enum comments explaining business rules

- **Testing**: No unit tests or integration tests found in codebase (recommendation: add)

### **3.7 DEPLOYMENT & ENVIRONMENT REQUIREMENTS**

#### FR-3.7.1 Development Environment
- .NET 8.0 runtime
- SQL Server (LocalDB) instance
- Visual Studio or VS Code
- Azure Storage (optional, for image uploads in production)

#### FR-3.7.2 Production Environment
- **HTTPS enforcement**: Required for payment security
- **Database**: SQL Server 2019+ recommended
- **Web server**: IIS or Docker container
- **Logging**: Application Insights or similar for monitoring
- **Error tracking**: Exception logging to file or cloud service
- **Session persistence**: Redis or distributed cache (instead of in-memory cache)

#### FR-3.7.3 Configuration Management
- **appsettings.json**: Base configuration
- **appsettings.Development.json**: Development overrides
- **Environment variables**: Used for sensitive data in deployment
- **Secrets**: Should use Azure Key Vault or similar (currently in config files)

---

## **4. USER ROLES & PERMISSIONS**

### **4.1 Customer Role**
| Feature | Permission |
|---------|-----------|
| Browse Products | ✓ Full |
| View Product Details | ✓ Full |
| Search & Filter | ✓ Full |
| View Categories | ✓ Full |
| Register Account | ✓ Yes |
| Login | ✓ Yes |
| Manage Profile | ✓ Own profile only |
| Shopping Cart | ✓ Own cart only |
| View My Orders | ✓ Own orders only |
| Place Orders (COD) | ✓ Yes |
| Online Payment | ✓ Yes (VNPay, Momo) |
| Apply Coupons | ✓ Yes |
| Submit Contact Form | ✓ Yes |
| API Access | ✓ Read-only |
| **Admin Panel** | ✗ Denied |

### **4.2 Admin Role**
| Feature | Permission |
|---------|-----------|
| All Customer Features | ✓ Yes |
| Create Products | ✓ Yes |
| Edit Products | ✓ Yes |
| Delete Products | ✓ Yes (soft-delete) |
| Upload Product Images | ✓ Yes |
| Create Categories | ✓ Yes |
| Edit Categories | ✓ Yes |
| Delete Categories | ✓ Yes (guarded) |
| View All Orders | ✓ Yes |
| Filter/Search Orders | ✓ Yes |
| Update Order Status | ✓ Yes |
| View Order Details | ✓ Yes |
| Create Coupons | ✓ Yes |
| Edit Coupons | ✓ Yes |
| Delete Coupons | ✓ Yes |
| List Users | ✓ Yes (read-only) |
| Admin Dashboard | ✓ Yes |

### **4.3 Anonymous (Unauthenticated) User**
| Feature | Permission |
|---------|-----------|
| Browse Products | ✓ Full |
| Search & Filter | ✓ Full |
| View Details | ✓ Yes |
| Shopping Cart | ✗ Denied |
| Register | ✓ Yes |
| Login | ✓ Yes |
| Payment Callbacks | ✓ Allowed (no auth required) |
| Contact Form | ✓ Yes |
| API Access | ✓ Read-only |

---

## **5. DATA MODELS & ENTITIES**

### **5.1 Entity Relationship Diagram (Conceptual)**

```
ApplicationUser (1) -----> (*) CartItem
ApplicationUser (1) -----> (*) Order
Category (1) -----------> (*) Product
Product (1) -----------> (*) CartItem
Product (1) -----------> (*) OrderDetail
Order (1) -----------> (*) OrderDetail
Coupon (*) <--------- (*) Order
```

### **5.2 Core Entities**

#### **ApplicationUser**
```
Primary Key: Id (string)
- UserName: string (from Identity)
- Email: string (unique, required)
- PasswordHash: string (hashed)
- FullName: string
- Address: string? (nullable)
- PhoneNumber: string? (nullable, from Identity)
- EmailConfirmed: bool
- LockoutEnabled, LockoutEnd (for login attempts)

Relationships:
  - Orders: ICollection<Order>
  - CartItems: ICollection<CartItem>
  - AspNetUserRoles: Role assignments
```

#### **Product**
```
Primary Key: Id (int)
- Name: string (200 max, required)
- Description: string? (2000 max, nullable)
- Price: decimal(18,2) (required, base price)
- DiscountPercent: decimal(5,2)? (0-100%, nullable)
- Stock: int (quantity on hand)
- ImageUrl: string? (500 max)
- CategoryId: int (foreign key)

Calculated Field:
  - EffectivePrice = Price × (100 - DiscountPercent) / 100

Relationships:
  - Category: Category (required)
  - CartItems: ICollection<CartItem>
  - OrderDetails: ICollection<OrderDetail> (implicit via Product data storage)
```

#### **Category**
```
Primary Key: Id (int)
- Name: string (120 max, required)
- Description: string? (500 max, nullable)

Relationships:
  - Products: ICollection<Product>
```

#### **CartItem**
```
Primary Key: Id (int)
- UserId: string (foreign key to ApplicationUser)
- ProductId: int (foreign key to Product)
- Quantity: int (1-9999 range)

Unique Constraint: (UserId, ProductId) - one line per product per user

Relationships:
  - User: ApplicationUser
  - Product: Product
```

#### **Order**
```
Primary Key: Id (int)
- UserId: string (foreign key, required)
- OrderDate: DateTime (UTC, defaults to DateTime.UtcNow)
- ShippingName: string (120 max, required)
- ShippingPhone: string (20 max, required)
- ShippingAddress: string (500 max, required)
- SubtotalAmount: decimal(18,2) (before coupon)
- CouponDiscountAmount: decimal(18,2) (coupon discount value)
- TotalAmount: decimal(18,2) (final price = subtotal - coupon_discount)
- Status: OrderStatus enum (Pending, Processing, Shipped, Delivered, Cancelled, AwaitingPayment)
- PaymentMethod: PaymentMethod enum (Cod, VNPay, Momo)
- PaymentStatus: PaymentStatus enum (None, Pending, Paid, Failed)
- CouponId: int? (foreign key, nullable)
- AppliedCouponCode: string? (denormalized for reference)
- GatewayTxnRef: string? (100 max, transaction ID for payment gateway)

Relationships:
  - User: ApplicationUser (required)
  - OrderDetails: ICollection<OrderDetail>
  - Coupon: Coupon? (optional)
```

#### **OrderDetail**
```
Primary Key: Id (int)
- OrderId: int (foreign key, required)
- ProductId: int (reference, denormalized)
- ProductName: string (200 max, denormalized snapshot)
- UnitPrice: decimal(18,2) (effective price at time of order)
- Quantity: int

Relationships:
  - Order: Order (with cascade delete)
```

#### **Coupon**
```
Primary Key: Id (int)
- Code: string (40 max, uppercase, unique, required)
- DiscountType: CouponDiscountType enum (Percent, FixedAmount)
- Value: decimal(18,2) (percentage 0-100 or fixed amount in VND)
- MinOrderValue: decimal(18,2)? (nullable)
- MaxDiscountAmount: decimal(18,2)? (cap for percentage, nullable)
- StartDateUtc: DateTime (start of validity period in UTC)
- EndDateUtc: DateTime (end of validity period in UTC)
- UsageLimit: int? (max uses, nullable = unlimited)
- UsedCount: int (current uses tracking)
- IsActive: bool (soft deactivation)

Unique Constraint: Code
Indexes: Code (for fast lookup)
```

### **5.3 Enumerations**

#### **OrderStatus**
```
Pending = 0         // Initial order state
Processing = 1      // Being prepared
Shipped = 2         // In transit
Delivered = 3       // Received by customer
Cancelled = 4       // Order cancelled
AwaitingPayment = 5 // Pending online payment completion
```

#### **PaymentMethod**
```
Cod = 0             // Cash on Delivery
VNPay = 1           // VNPay gateway
Momo = 2            // Momo wallet
```

#### **PaymentStatus**
```
None = 0            // No payment (COD)
Pending = 1         // Awaiting payment
Paid = 2            // Successful payment
Failed = 3          // Payment failed
```

#### **CouponDiscountType**
```
Percent = 0         // Percentage discount
FixedAmount = 1     // Fixed amount (VND)
```

### **5.4 ViewModels**

#### **ProductFormViewModel** (Admin)
```
- Id: int
- Name: string
- Description: string?
- Price: decimal
- DiscountPercent: decimal?
- Stock: int
- CategoryId: int
- ImageFile: IFormFile? (file upload)
- ExistingImageUrl: string? (current image)
- Categories: List<Category> (for dropdown)
```

#### **ProductListViewModel** (Customer)
```
- Paged: PagedResult<Product>
- Categories: List<Category>
- Pagination: PaginationViewModel
- Query: string? (search term)
- CategoryId: int?
- MinPrice: decimal?
- MaxPrice: decimal?
- Sort: string
```

#### **CheckoutViewModel**
```
- ShippingName: string
- ShippingPhone: string
- ShippingAddress: string
- PaymentKind: CheckoutPaymentKind enum (Cod, VNPay, Momo)
- CouponCodeInput: string? (to apply coupon)
```

#### **CouponFormViewModel** (Admin)
```
- Id: int
- Code: string
- DiscountType: CouponDiscountType
- Value: decimal
- MinOrderValue: decimal?
- MaxDiscountAmount: decimal?
- StartDateLocal: DateTime (Vietnam timezone)
- EndDateLocal: DateTime
- UsageLimit: int?
- IsActive: bool
- UsedCount: int (read-only display)
```

#### **LoginViewModel**
```
- Email: string
- Password: string
- RememberMe: bool
- ReturnUrl: string?
```

#### **RegisterViewModel**
```
- Email: string
- Password: string
- FullName: string
- Address: string?
```

#### **ProfileViewModel**
```
- Email: string (read-only display)
- FullName: string
- PhoneNumber: string?
- Address: string?
```

#### **ContactViewModel**
```
- Name: string
- Email: string
- Phone: string?
- Message: string
```

#### **PagedResult<T>**
```
- Items: List<T>
- Page: int (current page)
- PageSize: int (items per page)
- TotalCount: int (total items)
```

---

## **6. EXTERNAL INTERFACES**

### **6.1 VNPay Payment Gateway**

#### **Integration Points**
- **Service class**: `VnpayService` implementing `IVnpayService`
- **Configuration**: `VnpayOptions` from appsettings
- **Endpoints**:
  - Payment URL: `https://sandbox.vnpayment.vn/paymentv2/vpcpay.html` (sandbox)
  - IPN endpoint (optional): Server callback for payment confirmation

#### **Payment Request Parameters**
| Parameter | Description | Example |
|-----------|-------------|---------|
| vnp_Version | API version | 2.1.0 |
| vnp_TmnCode | Terminal code | XXXXXX |
| vnp_Command | Operation | pay |
| vnp_Amount | Amount × 100 | 5000000 (500,000 VND) |
| vnp_CreateDate | Start time | yyyyMMddHHmmss |
| vnp_ExpireDate | Expiration | yyyyMMddHHmmss (15 min from creation) |
| vnp_TxnRef | Transaction ID | {orderId}_{datetime} |
| vnp_CurrCode | Currency | VND |
| vnp_OrderInfo | Description | Thanh toan don hang #{orderId} |
| vnp_OrderType | Type | other |
| vnp_ReturnUrl | Return URL | https://yourdomain.com/Payment/VnpayReturn |
| vnp_IpnUrl | IPN webhook | https://yourdomain.com/Payment/VnpayIpn |
| vnp_SecureHash | HMAC-SHA512 | Lowercase hex string |

#### **Return Parameters**
| Parameter | Description |
|-----------|-------------|
| vnp_ResponseCode | 00 = success, others = fail |
| vnp_TransactionStatus | 00 = completed |
| vnp_TransactionNo | VNPay transaction reference |
| vnp_BankCode | Issuing bank code |
| vnp_SecureHash | Signature for validation |

#### **Signature Algorithm**
```
Data = sorted_params (excluding vnp_SecureHash)
Signature = HMAC-SHA512(key=HashSecret, data=Data) → lowercase hex
```

### **6.2 Momo Payment Gateway**

#### **Integration Points**
- **Service class**: `MomoPaymentService` implementing `IMomoPaymentService`
- **Configuration**: `MomoOptions` from appsettings
- **HTTP client**: Named client for payment API calls

#### **Create Payment Request (HTTP POST)**
| Parameter | Type | Description |
|-----------|------|-------------|
| partnerCode | string | Merchant code |
| partnerName | string | "ECommerceWeb" |
| storeId | string | "ECommerceStore" |
| requestId | string | Unique per request (RequestId + HHmmss) |
| amount | string | Amount in VND |
| orderId | string | Transaction ID (matches Order GatewayTxnRef) |
| orderInfo | string | "Thanh toan don hang #{orderId}" |
| redirectUrl | string | Return URL after payment |
| ipnUrl | string | Webhook for notifications |
| lang | string | "vi" for Vietnamese |
| requestType | string | "captureWallet" |
| extraData | string | Empty or additional data |
| signature | string | HMAC-SHA256 signature |

#### **Create Payment Response**
```json
{
  "partnerCode": "...",
  "requestId": "...",
  "orderId": "...",
  "errorCode": "0|non-zero",
  "orderGroupId": "...",
  "message": "Giao dịch thành công|error message",
  "responseTime": "...",
  "payUrl": "https://...",  // QR code or redirect URL
  "deeplink": "...",
  "qrCodeUrl": "..."
}
```

---

## **7. ASSUMPTIONS AND CONSTRAINTS**

### **7.1 Assumptions**
- Users have basic computer literacy for web browsing
- Internet connectivity is available for online payments
- Payment gateways (VNPay, Momo) are operational and accessible
- SQL Server database is always available
- File system has sufficient space for product images
- Email addresses are valid and accessible (for future email features)
- Vietnam timezone is the primary operational timezone
- VND is the only supported currency

### **7.2 Constraints**
- **Technical Constraints**:
  - ASP.NET Core 8.0 minimum requirement
  - SQL Server database (LocalDB for development)
  - Windows environment for development (based on PowerShell scripts)
  - HTTPS required for production payment processing
  - File upload size limited by server configuration
- **Business Constraints**:
  - Products cannot have negative stock
  - Orders cannot be modified after creation
  - Coupons are single-use per order (not stackable)
  - Admin role assignment requires manual database intervention
  - Contact form does not send actual emails (UI placeholder)
- **Regulatory Constraints**:
  - Payment processing must comply with Vietnamese financial regulations
  - User data protection (PII handling)
  - Secure payment data transmission
- **Performance Constraints**:
  - Page load time < 2 seconds
  - API response time < 200ms
  - Support for concurrent users (not quantified)
- **Security Constraints**:
  - No plaintext password storage
  - CSRF protection on all forms
  - Input validation on all user inputs
  - Secure payment gateway integration