
create database farmacia
use farmacia

create table proveedor (
    id int primary key identity(1,1),
    nombre varchar(100) not null,
    telefono varchar(15),
    email varchar(50),
    direccion varchar(150)
)


create table medicamento (
    id int primary key identity(1,1),
    nombre varchar(100) not null,
    descripcion varchar(200),
    tipo varchar(30), 
    precio_compra decimal(8,2) not null,
    precio_venta decimal(8,2) not null,
    proveedor_id int,
    foreign key (proveedor_id) references proveedor(id)
)


create table inventario (
    id int primary key identity(1,1),
    medicamento_id int not null,
    cantidad int not null,
    fecha_vencimiento date not null,
    lote varchar(20),
    stock_minimo int default 5,
    foreign key (medicamento_id) references medicamento(id)
)

  
create table venta (
    id int primary key identity(1,1),
    fecha datetime default getdate(),
    cliente varchar(50),
    total decimal(8,2) not null
)


create table detalle_venta (
    id int primary key identity(1,1),
    venta_id int not null,
    medicamento_id int not null,
    cantidad int not null,
    precio decimal(8,2) not null,
    foreign key (venta_id) references venta(id),
    foreign key (medicamento_id) references medicamento(id)
)


insert into proveedor (nombre, telefono, email, direccion) values 
('Laboratorios kiriku', '8095551234', 'info@abc.com', 'Santo Domingo'),
('Distribuidora Guachupita', '8095555678', 'ventas@xyz.com', 'Santiago'),
('Farmacia Matasanos', '8095559999', 'compras@central.com', 'La Romana')


insert into medicamento (nombre, descripcion, tipo, precio_compra, precio_venta, proveedor_id) values
('Acetaminofen', 'Para dolor y fiebre', 'Pastilla', 30.00, 50.00, 1),
('Ibuprofeno', 'Antiinflamatorio', 'Pastilla', 40.00, 70.00, 1),
('Jarabe pa la to', 'Pa la to', 'Jarabe', 60.00, 100.00, 2),
('Vitamina C', 'Suplemento vitamina', 'Pastilla', 25.00, 45.00, 2),
('Aspirina', 'Para dolor de cabeza', 'Pastilla', 20.00, 35.00, 3)


insert into inventario (medicamento_id, cantidad, fecha_vencimiento, lote, stock_minimo) values
(1, 50, '2025-12-31', 'L001', 10),
(1, 30, '2026-03-15', 'L002', 10),
(2, 5, '2025-10-20', 'L003', 15),
(3, 25, '2025-09-10', 'L004', 8),  
(4, 40, '2026-01-30', 'L005', 12),
(5, 2, '2025-08-25', 'L006', 10)   


insert into venta (cliente, total) values
('Maria Zapatico', 120.00),
('Juan Panza', 200.00),
('Ana Garabito', 85.00)

insert into detalle_venta (venta_id, medicamento_id, cantidad, precio) values
(1, 1, 2, 50.00),
(1, 4, 1, 45.00),
(2, 2, 2, 70.00),
(2, 3, 1, 100.00),
(3, 5, 1, 35.00),
(3, 1, 1, 50.00)


select * from medicamento


select 
    m.nombre as medicamento,
    i.cantidad,
    i.fecha_vencimiento,
    i.lote,
    p.nombre as proveedor
from inventario i
join medicamento m on i.medicamento_id = m.id
join proveedor p on m.proveedor_id = p.id
order by m.nombre


select 
    m.nombre as medicamento,
    i.cantidad as stock_actual,
    i.stock_minimo,
    'STOCK BAJO!' as alerta
from inventario i
join medicamento m on i.medicamento_id = m.id
where i.cantidad <= i.stock_minimo


select 
    m.nombre as medicamento,
    i.fecha_vencimiento,
    i.cantidad,
    datediff(day, getdate(), i.fecha_vencimiento) as dias_para_vencer,
    'SE VENCE ORITA!' as alerta
from inventario i
join medicamento m on i.medicamento_id = m.id
where i.fecha_vencimiento <= dateadd(day, 30, getdate())
and i.cantidad > 0

select 
    v.id as venta_numero,
    v.cliente,
    v.fecha,
    v.total
from venta v
where cast(v.fecha as date) = cast(getdate() as date)


select 
    v.cliente,
    m.nombre as medicamento,
    dv.cantidad,
    dv.precio,
    (dv.cantidad * dv.precio) as subtotal
from venta v
join detalle_venta dv on v.id = dv.venta_id
join medicamento m on dv.medicamento_id = m.id
where v.id = 1


select 'STOCK BAJO' as tipo_alerta, m.nombre as medicamento, 
       cast(i.cantidad as varchar) as detalle
from inventario i
join medicamento m on i.medicamento_id = m.id
where i.cantidad <= i.stock_minimo

union all

select 'VENCE ORITA' as tipo_alerta, m.nombre as medicamento,
       cast(i.fecha_vencimiento as varchar) as detalle  
from inventario i
join medicamento m on i.medicamento_id = m.id
where i.fecha_vencimiento <= dateadd(day, 30, getdate())
and i.cantidad > 0

select 
    m.nombre,
    sum(i.cantidad) as total_disponible,
    m.precio_venta
from medicamento m
join inventario i on m.id = i.medicamento_id
where i.cantidad > 0
group by m.nombre, m.precio_venta
order by m.nombre
