-- Publish as: Module:The Story of Red Cloud/Armor
local p = {}
local items = mw.loadData("Module:The Story of Red Cloud/Items/data")
local sets = mw.loadData("Module:The Story of Red Cloud/Armor sets/data")

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

local function appendPieceDetails(lines, slot, item)
    table.insert(lines, "=== " .. slot .. " ===")
    if item.tooltip and item.tooltip ~= "" then
        table.insert(lines, text(item.tooltip):gsub("\n", "<br />"))
    end
    if item.recipes and #item.recipes > 0 then
        table.insert(lines, "'''Recipes'''" )
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
        table.insert(lines, "'''Drop sources'''" )
        for _, source in ipairs(item.dropSources) do
            local stack = source.minStack == source.maxStack and source.minStack or source.minStack .. "–" .. source.maxStack
            table.insert(lines, "* " .. text(source.name) .. " — " .. percent(source.chancePercent) .. ", " .. stack .. conditions(source.conditions))
        end
    end
end

function p.main(frame)
    local id = arg(frame, "id")
    local set = sets[id]
    if not set then
        return "<strong class=\"error\">Unknown TSORC armor set ID: " .. text(id) .. "</strong>"
    end

    local pieces = {}
    for _, id in ipairs(set.heads or {}) do
        table.insert(pieces, { slot = "Helmet", id = id })
    end
    for _, id in ipairs(set.bodies or {}) do
        table.insert(pieces, { slot = "Chestpiece", id = id })
    end
    for _, id in ipairs(set.legs or {}) do
        table.insert(pieces, { slot = "Leggings", id = id })
    end
    local lines = {
        "{| class=\"wikitable\" style=\"margin:auto; text-align:center\"",
        "|+ " .. text(set.wikiName),
        "|-\n! Slot\n! Item\n! Defense",
    }
    for _, piece in ipairs(pieces) do
        local item = items[piece.id]
        table.insert(lines, "|-\n| " .. text(piece.slot) .. "\n| [[File:" .. item.image .. "|100px|alt=" .. text(item.wikiName) .. "]]" .. "<br />'''" .. text(item.wikiName) .. "'''\n| " .. tostring(item.defense))
    end
    table.insert(lines, "|}")

    for _, piece in ipairs(pieces) do
        appendPieceDetails(lines, piece.slot .. " — " .. items[piece.id].wikiName, items[piece.id])
    end
    return frame:preprocess(table.concat(lines, "\n"))
end

return p
