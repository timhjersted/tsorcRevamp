-- Publish as: Module:The Story of Red Cloud/Item
local p = {}
local data = mw.loadData("Module:The Story of Red Cloud/Items/data")

local function arg(frame, name)
    return frame.args[name] or frame:getParent().args[name]
end

local function text(value)
    return mw.text.nowiki(tostring(value or ""))
end

local function percent(value)
    return string.format("%.3g%%", tonumber(value) or 0)
end

local function conditions(values)
    if not values or #values == 0 then
        return ""
    end
    return " <small>(" .. text(table.concat(values, "; ")) .. ")</small>"
end

local function coins(value)
    value = tonumber(value) or 0
    if value <= 0 then
        return "—"
    end
    local platinum = math.floor(value / 1000000)
    value = value % 1000000
    local gold = math.floor(value / 10000)
    value = value % 10000
    local silver = math.floor(value / 100)
    local copper = value % 100
    local parts = {}
    if platinum > 0 then table.insert(parts, platinum .. " platinum") end
    if gold > 0 then table.insert(parts, gold .. " gold") end
    if silver > 0 then table.insert(parts, silver .. " silver") end
    if copper > 0 then table.insert(parts, copper .. " copper") end
    return table.concat(parts, ", ")
end

local function stat(lines, label, value)
    if value ~= nil and value ~= "" and value ~= 0 and value ~= -1 then
        table.insert(lines, "|-\n! " .. label .. "\n| " .. text(value))
    end
end

function p.main(frame)
    local id = arg(frame, "id")
    local item = data[id]
    if not item then
        return "<strong class=\"error\">Unknown TSORC item ID: " .. text(id) .. "</strong>"
    end

    local lines = {
        "[[File:" .. item.image .. "|160px|alt=" .. text(item.wikiName) .. "]]",
        "{| class=\"wikitable\" style=\"float:right; clear:right; margin-left:1em; min-width:18em\"",
        "|+ " .. text(item.wikiName),
        "|-\n! Type\n| " .. text(item.type),
        "|-\n! Rarity\n| " .. text(item.rare),
        "|-\n! Sell value\n| " .. coins(item.value),
    }
    stat(lines, "Damage", item.damage and item.damage > 0 and item.damage .. " " .. item.damageClass or nil)
    stat(lines, "Critical chance", item.crit and item.crit > 0 and item.crit .. "%" or nil)
    stat(lines, "Knockback", item.knockback)
    stat(lines, "Defense", item.defense)
    stat(lines, "Mana", item.mana)
    stat(lines, "Use time", item.useTime)
    stat(lines, "Healing", item.healLife and item.healLife > 0 and item.healLife .. " life" or nil)
    stat(lines, "Mana restored", item.healMana and item.healMana > 0 and item.healMana .. " mana" or nil)
    table.insert(lines, "|}")

    if item.tooltip and item.tooltip ~= "" then
        table.insert(lines, "== Description ==\n" .. text(item.tooltip):gsub("\n", "<br />"))
    end

    if item.recipes and #item.recipes > 0 then
        table.insert(lines, "== Recipes ==")
        for _, recipe in ipairs(item.recipes) do
            local ingredients = {}
            for _, ingredient in ipairs(recipe.ingredients or {}) do
                local name = ingredient.recipeGroup ~= "" and ingredient.recipeGroup or ingredient.itemName
                table.insert(ingredients, text(name) .. " ×" .. tostring(ingredient.stack or 1))
            end
            local stations = {}
            for _, station in ipairs(recipe.stations or {}) do
                table.insert(stations, text(station.name))
            end
            local craftedAt = #stations > 0 and " at " .. table.concat(stations, ", ") or ""
            table.insert(lines, "* " .. table.concat(ingredients, ", ") .. craftedAt .. conditions(recipe.conditions))
        end
    end

    if item.dropSources and #item.dropSources > 0 then
        table.insert(lines, "== Drop sources ==")
        for _, source in ipairs(item.dropSources) do
            local stack = source.minStack == source.maxStack and source.minStack or source.minStack .. "–" .. source.maxStack
            table.insert(lines, "* " .. text(source.name) .. " — " .. percent(source.chancePercent) .. ", " .. stack .. conditions(source.conditions))
        end
    end

    if item.shopSources and #item.shopSources > 0 then
        table.insert(lines, "== Sold by ==")
        for _, shop in ipairs(item.shopSources) do
            local price = shop.currencyId == 0 and coins(shop.price) or tostring(shop.price) .. " (custom currency)"
            table.insert(lines, "* " .. text(shop.npcName) .. " (" .. text(shop.shopName) .. ") — " .. price .. conditions(shop.conditions))
        end
    end

    return frame:preprocess(table.concat(lines, "\n"))
end

return p
