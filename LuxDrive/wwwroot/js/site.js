/* Бутонът с трите точки */
.card - menu - btn {
    position: absolute;
    top: 12px;
    right: 12px;
    background: transparent;
    border: none;
    font - size: 18px;
    cursor: pointer;
    color: #d4af37;
    z - index: 20;
}

/* Самото меню */
.dropdown - menu {
    position: absolute;
    right: 10px;
    top: 45px;
    background - color: #1a1a1a;
    border - radius: 10px;
    padding: 8px 0;
    width: 160px;
    display: none;
    box - shadow: 0 8px 18px rgba(0, 0, 0, 0.6);
    z - index: 999;
}

/* Когато JS добави .show → менюто се показва */
.dropdown - menu.show {
    display: block;
}

/* Елементи в менюто */
.menu - item {
    display: block;
    padding: 10px 15px;
    color: white;
    text - decoration: none;
    font - size: 14px;
    transition: 0.15s;
}

.menu - item i {
    margin - right: 8px;
    color: #d4af37;
}

.menu - item:hover {
    background - color: #333;
}

/* Червено за Изтрий */
.menu - item.delete {
    color: #ff6b6b;
}

.menu - item.delete i {
    color: #ff6b6b;
}