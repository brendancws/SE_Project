SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    -- Existing menu items
    UPDATE dbo.MenuItem
    SET ImageURL = '/images/menu/nasi-lemak.webp'
    WHERE Name = 'Nasi Lemak';

    UPDATE dbo.MenuItem
    SET ImageURL = '/images/menu/fried-rice.webp'
    WHERE Name = 'Fried Rice';

    UPDATE dbo.MenuItem
    SET ImageURL = '/images/menu/teh-tarik.webp'
    WHERE Name = 'Teh Tarik';

    UPDATE dbo.MenuItem
    SET ImageURL = '/images/menu/iced-lemon-tea.webp'
    WHERE Name = 'Iced Lemon Tea';

    UPDATE dbo.MenuItem
    SET ImageURL = '/images/menu/cendol.webp'
    WHERE Name = 'Cendol';

    -- CategoryID 1 = Mains
    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.MenuItem
        WHERE Name = 'Chicken Chop'
    )
    BEGIN
        INSERT INTO dbo.MenuItem
        (
            CategoryID,
            Name,
            Description,
            Price,
            Availability,
            ImageURL
        )
        VALUES
        (
            1,
            'Chicken Chop',
            'Grilled chicken chop served with fries and black pepper sauce.',
            16.90,
            1,
            '/images/menu/chicken-chop.webp'
        );
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.MenuItem
        WHERE Name = 'Mee Goreng Mamak'
    )
    BEGIN
        INSERT INTO dbo.MenuItem
        (
            CategoryID,
            Name,
            Description,
            Price,
            Availability,
            ImageURL
        )
        VALUES
        (
            1,
            'Mee Goreng Mamak',
            'Wok-fried noodles with vegetables, tofu, and savoury sauce.',
            10.90,
            1,
            '/images/menu/mee-goreng-mamak.webp'
        );
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.MenuItem
        WHERE Name = 'Curry Laksa'
    )
    BEGIN
        INSERT INTO dbo.MenuItem
        (
            CategoryID,
            Name,
            Description,
            Price,
            Availability,
            ImageURL
        )
        VALUES
        (
            1,
            'Curry Laksa',
            'Noodles served in a rich coconut curry broth with traditional toppings.',
            13.90,
            1,
            '/images/menu/curry-laksa.webp'
        );
    END;

    -- CategoryID 2 = Drinks
    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.MenuItem
        WHERE Name = 'Milo Ais'
    )
    BEGIN
        INSERT INTO dbo.MenuItem
        (
            CategoryID,
            Name,
            Description,
            Price,
            Availability,
            ImageURL
        )
        VALUES
        (
            2,
            'Milo Ais',
            'Chilled chocolate malt drink served over ice.',
            5.50,
            1,
            '/images/menu/milo-ais.webp'
        );
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.MenuItem
        WHERE Name = 'Sirap Bandung'
    )
    BEGIN
        INSERT INTO dbo.MenuItem
        (
            CategoryID,
            Name,
            Description,
            Price,
            Availability,
            ImageURL
        )
        VALUES
        (
            2,
            'Sirap Bandung',
            'Refreshing rose syrup drink mixed with milk and served over ice.',
            4.80,
            1,
            '/images/menu/sirap-bandung.webp'
        );
    END;

    -- CategoryID 3 = Desserts
    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.MenuItem
        WHERE Name = 'Ais Kacang'
    )
    BEGIN
        INSERT INTO dbo.MenuItem
        (
            CategoryID,
            Name,
            Description,
            Price,
            Availability,
            ImageURL
        )
        VALUES
        (
            3,
            'Ais Kacang',
            'Shaved ice dessert with syrup, sweet corn, beans, and colourful toppings.',
            7.50,
            1,
            '/images/menu/ais-kacang.webp'
        );
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.MenuItem
        WHERE Name = 'Sago Gula Melaka'
    )
    BEGIN
        INSERT INTO dbo.MenuItem
        (
            CategoryID,
            Name,
            Description,
            Price,
            Availability,
            ImageURL
        )
        VALUES
        (
            3,
            'Sago Gula Melaka',
            'Sago pearls served with coconut milk and palm sugar syrup.',
            6.90,
            1,
            '/images/menu/sago-gula-melaka.webp'
        );
    END;

    -- Ensure all added items have the expected image paths
    UPDATE dbo.MenuItem
    SET ImageURL = '/images/menu/chicken-chop.webp'
    WHERE Name = 'Chicken Chop';

    UPDATE dbo.MenuItem
    SET ImageURL = '/images/menu/mee-goreng-mamak.webp'
    WHERE Name = 'Mee Goreng Mamak';

    UPDATE dbo.MenuItem
    SET ImageURL = '/images/menu/curry-laksa.webp'
    WHERE Name = 'Curry Laksa';

    UPDATE dbo.MenuItem
    SET ImageURL = '/images/menu/milo-ais.webp'
    WHERE Name = 'Milo Ais';

    UPDATE dbo.MenuItem
    SET ImageURL = '/images/menu/sirap-bandung.webp'
    WHERE Name = 'Sirap Bandung';

    UPDATE dbo.MenuItem
    SET ImageURL = '/images/menu/ais-kacang.webp'
    WHERE Name = 'Ais Kacang';

    UPDATE dbo.MenuItem
    SET ImageURL = '/images/menu/sago-gula-melaka.webp'
    WHERE Name = 'Sago Gula Melaka';

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;

    THROW;
END CATCH;